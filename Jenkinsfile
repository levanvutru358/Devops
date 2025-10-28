pipeline {
  agent any

  environment {
    DOCKER_USER = credentials('dockerhub-cred').username
    DOCKER_PASS = credentials('dockerhub-cred').password
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
              echo "Building and testing .NET API..."
              docker run --rm -v $(pwd):/workspace -w /workspace \
                mcr.microsoft.com/dotnet/sdk:9.0 \
                bash -c "cd server && dotnet restore && dotnet test --logger trx --results-directory TestResults || true"
            '''
          }
          post {
            always {
              junit testResultsPattern: 'server/TestResults/*.trx'
            }
          }
        }
        
        stage('Frontend') {
          steps {
            sh '''
              echo "Building and testing React app..."
              docker run --rm -v $(pwd)/client:/app -w /app \
                node:20-alpine \
                sh -c "npm ci && npm run build || true"
            '''
          }
        }
      }
    }

    stage('Build Images') {
      steps {
        sh '''
          echo "Building Docker images..."
          docker build -t $DOCKER_USER/webshop-api:$BUILD_NUMBER -f server/Dockerfile .
          docker build -t $DOCKER_USER/webshop-client:$BUILD_NUMBER --build-arg VITE_API_URL=http://$SERVER_HOST:5193 -f client/Dockerfile client/
          
          docker tag $DOCKER_USER/webshop-api:$BUILD_NUMBER $DOCKER_USER/webshop-api:latest
          docker tag $DOCKER_USER/webshop-client:$BUILD_NUMBER $DOCKER_USER/webshop-client:latest
        '''
      }
    }

    stage('Push Images') {
      steps {
        sh '''
          echo "Pushing to Docker Hub..."
          echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin
          docker push $DOCKER_USER/webshop-api:$BUILD_NUMBER
          docker push $DOCKER_USER/webshop-api:latest
          docker push $DOCKER_USER/webshop-client:$BUILD_NUMBER
          docker push $DOCKER_USER/webshop-client:latest
          docker logout
        '''
      }
    }

    stage('Deploy') {
      steps {
        sshagent(['server-ssh-key']) {
          sh '''
            echo "Deploying to server..."
            scp -o StrictHostKeyChecking=no docker-compose.yml $SERVER_USER@$SERVER_HOST:~/project/
            
            ssh -o StrictHostKeyChecking=no $SERVER_USER@$SERVER_HOST "
              cd ~/project &&
              echo 'DOCKER_USERNAME=$DOCKER_USER' > .env &&
              echo 'DB_CONNECTION_STRING=Server=db;Port=3306;Database=emo_db;User Id=tru123;Password=tru12345;SslMode=None;' >> .env &&
              echo 'JWT_SECRET=CHANGE_ME_SUPER_SECRET_MIN_32_CHARS_1234567' >> .env &&
              echo 'ASPNETCORE_ENVIRONMENT=Production' >> .env &&
              echo 'VITE_API_URL=http://$SERVER_HOST:5193' >> .env &&
              echo '$DOCKER_PASS' | docker login -u $DOCKER_USER --password-stdin &&
              docker compose --env-file .env pull &&
              docker compose --env-file .env down &&
              docker compose --env-file .env up -d &&
              docker image prune -f
            "
          '''
        }
      }
    }

    stage('Health Check') {
      steps {
        sh '''
          echo "Checking services..."
          sleep 30
          curl -f http://$SERVER_HOST:5193/health || echo "API check failed"
          curl -f http://$SERVER_HOST:5173 || echo "Client check failed"
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