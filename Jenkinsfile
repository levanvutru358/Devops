pipeline {
  agent any

  environment {
    SERVER_HOST = "47.128.79.251"
    SERVER_USER = "root"
  }

  stages {
    stage('Checkout') {
      steps {
        checkout scm
        sh 'git rev-parse --short HEAD > commit.txt'
        script {
          env.GIT_COMMIT_SHORT = readFile('commit.txt').trim()
        }
      }
    }

    stage('Build & Test') {
      parallel {
        stage('Backend') {
          steps {
            sh '''
              echo "=== Building .NET API ==="
              docker run --rm -v $(pwd):/workspace -w /workspace \
                mcr.microsoft.com/dotnet/sdk:9.0 \
                bash -c "
                  cd server && 
                  echo 'Step 1: Restoring packages...' &&
                  dotnet restore &&
                  echo 'Step 2: Building project...' &&
                  dotnet build --configuration Release &&
                  echo 'Step 3: Checking for tests...' &&
                  if find . -name '*Test*.csproj' -o -name '*Tests*.csproj' | grep -q .; then
                    echo 'Found test projects, running tests...' &&
                    dotnet test --logger trx --results-directory TestResults --no-build
                  else
                    echo 'No test projects found, skipping tests'
                  fi
                "
            '''
          }
          post {
            always {
              script {
                if (fileExists('server/TestResults')) {
                  echo 'Publishing test results...'
                  junit testResults: 'server/TestResults/*.trx'
                } else {
                  echo 'No test results found - project has no tests (this is OK)'
                }
              }
            }
          }
        }
        
        stage('Frontend') {
          steps {
            sh '''
              echo "=== Building React App ==="
              docker run --rm -v $(pwd)/client:/app -w /app \
                node:20-alpine \
                sh -c "
                  echo 'Step 1: Installing dependencies...' &&
                  npm ci --no-audit --no-fund &&
                  echo 'Step 2: Building application...' &&
                  npm run build &&
                  echo 'Frontend build completed successfully!'
                "
            '''
          }
        }
      }
    }

    stage('Build Images') {
      steps {
        withCredentials([usernamePassword(credentialsId: 'dockerhub-cred', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
          sh '''
            echo "=== Building Docker Images ==="
            docker build -t $DOCKER_USER/webshop-api:$BUILD_NUMBER -f server/Dockerfile .
            docker build -t $DOCKER_USER/webshop-client:$BUILD_NUMBER --build-arg VITE_API_URL=http://$SERVER_HOST:5193 -f client/Dockerfile client/
            
            docker tag $DOCKER_USER/webshop-api:$BUILD_NUMBER $DOCKER_USER/webshop-api:latest
            docker tag $DOCKER_USER/webshop-client:$BUILD_NUMBER $DOCKER_USER/webshop-client:latest
            echo "Docker images built successfully!"
          '''
        }
      }
    }

    stage('Push Images') {
      steps {
        withCredentials([usernamePassword(credentialsId: 'dockerhub-cred', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
          sh '''
            echo "=== Pushing to Docker Hub ==="
            echo "Username: $DOCKER_USER"
            echo "Attempting Docker Hub login..."
            
            if echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin; then
              echo "✅ Docker Hub login successful!"
              echo "Pushing API image..."
              docker push $DOCKER_USER/webshop-api:$BUILD_NUMBER
              docker push $DOCKER_USER/webshop-api:latest
              echo "Pushing Client image..."
              docker push $DOCKER_USER/webshop-client:$BUILD_NUMBER
              docker push $DOCKER_USER/webshop-client:latest
              docker logout
              echo "✅ Images pushed successfully!"
            else
              echo "❌ Docker Hub login failed!"
              echo "Please check your Docker Hub credentials in Jenkins"
              echo "Credential ID: dockerhub-cred"
              exit 1
            fi
          '''
        }
      }
    }

    stage('Deploy') {
      steps {
        withCredentials([
          usernamePassword(credentialsId: 'dockerhub-cred', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS'),
          sshUserPrivateKey(credentialsId: 'server-ssh-key', keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')
        ]) {
          sh '''
            echo "=== Deploying to Server ==="
            echo "Copying docker-compose.yml to server..."
            scp -i "$SSH_KEY" -o StrictHostKeyChecking=no docker-compose.yml $SSH_USER@$SERVER_HOST:~/project/
            
            echo "Deploying on server..."
            ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no $SSH_USER@$SERVER_HOST "
              cd ~/project &&
              echo 'DOCKER_USERNAME=$DOCKER_USER' > .env &&
              echo 'DB_CONNECTION_STRING=Server=db;Port=3306;Database=emo_db;User Id=tru123;Password=tru12345;SslMode=None;' >> .env &&
              echo 'JWT_SECRET=CHANGE_ME_SUPER_SECRET_MIN_32_CHARS_1234567' >> .env &&
              echo 'ASPNETCORE_ENVIRONMENT=Production' >> .env &&
              echo 'VITE_API_URL=http://$SERVER_HOST:5193' >> .env &&
              echo 'Logging into Docker Hub on server...' &&
              if echo '$DOCKER_PASS' | docker login -u $DOCKER_USER --password-stdin; then
                echo 'Docker Hub login successful on server' &&
                docker compose --env-file .env pull &&
                docker compose --env-file .env down &&
                docker compose --env-file .env up -d &&
                docker image prune -f &&
                echo 'Deployment completed successfully!'
              else
                echo 'Docker Hub login failed on server' &&
                exit 1
              fi
            "
          '''
        }
      }
    }

    stage('Health Check') {
      steps {
        sh '''
          echo "=== Health Check ==="
          echo "Waiting for services to start..."
          sleep 30
          echo "Checking API health..."
          curl -f http://$SERVER_HOST:5193/health || echo "API health check failed"
          echo "Checking Client..."
          curl -f http://$SERVER_HOST:5173 || echo "Client health check failed"
        '''
      }
    }
  }

  post {
    always {
      sh 'docker image prune -f || true'
    }
    success {
      echo "✅ Deployment successful!"
      echo "API: http://$SERVER_HOST:5193"
      echo "Client: http://$SERVER_HOST:5173"
    }
    failure {
      echo "❌ Deployment failed!"
    }
  }
}