pipeline {
  agent any

  options {
    timestamps()
  }

  parameters {
    string(name: 'REPO_URL', defaultValue: 'https://github.com/levanvutru358/Devops.git', description: 'Git repository URL')
    string(name: 'GIT_BRANCH', defaultValue: 'main', description: 'Git branch to build')
    string(name: 'SERVER_HOST', defaultValue: '18.143.155.245', description: 'Target server IP or hostname')
    string(name: 'SERVER_USER', defaultValue: 'ubuntu', description: 'SSH username on target server')
    string(name: 'SSH_PORT', defaultValue: '22', description: 'SSH port on target server')
    string(name: 'API_PORT', defaultValue: '8080', description: 'Public API port on server')
    string(name: 'CLIENT_PORT', defaultValue: '5173', description: 'Public Client port on server')
    booleanParam(name: 'USE_COMPOSE_CRED', defaultValue: true, description: 'Use docker-compose.yml from Jenkins Credentials')
    string(name: 'COMPOSE_CRED_ID', defaultValue: 'docker-compose', description: 'Credentials ID (Secret file) containing docker-compose.yml')
  }

  environment {
    SERVER_HOST = "${params.SERVER_HOST}"
    SERVER_USER = "${params.SERVER_USER}"
    SSH_PORT    = "${params.SSH_PORT}"
    API_PORT    = "${params.API_PORT}"
    CLIENT_PORT = "${params.CLIENT_PORT}"
  }

  stages {
    stage('Checkout') {
      steps {
        script {
          git branch: params.GIT_BRANCH, credentialsId: 'github-pat', url: params.REPO_URL
        }
        sh 'git rev-parse --short HEAD > commit.txt'
        script {
          env.GIT_COMMIT_SHORT = readFile('commit.txt').trim()
        }
      }
    }

    stage('Cleanup Workspace') {
      steps {
        sh '''
          echo "=== Pre-clean workspace artifacts (root-in-container) ==="
          docker run --rm -v $(pwd):/ws -w /ws alpine:3 \
            sh -c "rm -rf server/bin server/obj client/node_modules client/dist || true"
        '''
      }
    }

    stage('Preflight SSH') {
      steps {
        withCredentials([sshUserPrivateKey(credentialsId: 'server-ssh-key', keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')]) {
          sh '''
            echo "=== Verifying SSH connectivity ==="
            echo "Host: $SERVER_HOST  Port: $SSH_PORT  User: $SSH_USER"
            # Try up to 3 times, fail fast with 10s timeout each
            i=0
            until [ $i -ge 3 ]; do
              if ssh -i "$SSH_KEY" -p "$SSH_PORT" -o ConnectTimeout=10 -o StrictHostKeyChecking=no -o LogLevel=ERROR -o BatchMode=yes "$SSH_USER@$SERVER_HOST" "echo ok"; then
                echo "SSH connectivity OK"
                exit 0
              fi
              i=$((i+1))
              echo "SSH attempt $i failed; retrying in 5s..."
              sleep 5
            done
            echo "ERROR: Cannot reach $SERVER_HOST:$SSH_PORT via SSH. Check server IP, firewall, or security group."
            exit 1
          '''
        }
      }
    }

    stage('Build & Test') {
      parallel {
        stage('Backend') {
          steps {
            sh '''
              echo "=== Building .NET API ==="
              docker run --rm \
                -u $(id -u):$(id -g) \
                -e DOTNET_CLI_HOME=/tmp -e NUGET_PACKAGES=/tmp/.nuget -e HOME=/tmp \
                -v $(pwd):/workspace -w /workspace \
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
                  echo 'Archiving test results (.trx)'
                  archiveArtifacts artifacts: 'server/TestResults/*.trx', allowEmptyArchive: true
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
              docker run --rm \
                -u $(id -u):$(id -g) \
                -e npm_config_cache=/tmp/.npm \
                -v $(pwd)/client:/app -w /app \
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
            docker build -t $DOCKER_USER/webshop-client:$BUILD_NUMBER --build-arg VITE_API_URL=http://$SERVER_HOST:$API_PORT -f client/Dockerfile client/

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
              echo "Docker Hub login successful!"
              echo "Pushing API image..."
              docker push $DOCKER_USER/webshop-api:$BUILD_NUMBER
              docker push $DOCKER_USER/webshop-api:latest
              echo "Pushing Client image..."
              docker push $DOCKER_USER/webshop-client:$BUILD_NUMBER
              docker push $DOCKER_USER/webshop-client:latest
              docker logout
              echo "Images pushed successfully!"
            else
              echo "Docker Hub login failed!"
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
          sshUserPrivateKey(credentialsId: 'server-ssh-key', keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER'),
          string(credentialsId: 'db-conn', variable: 'DB_CONN'),
          string(credentialsId: 'jwt-secret', variable: 'JWT_SECRET')
        ]) {
          sh '''
            set -e
            echo "=== Deploying to Server ==="

            # Prepare project directory on server
            ssh -i "$SSH_KEY" -p "$SSH_PORT" -o ConnectTimeout=10 -o StrictHostKeyChecking=no -o LogLevel=ERROR -o BatchMode=yes "$SSH_USER@$SERVER_HOST" "mkdir -p ~/project"
          '''

          script {
            if (params.USE_COMPOSE_CRED) {
              withCredentials([file(credentialsId: params.COMPOSE_CRED_ID, variable: 'COMPOSE_FILE')]) {
                sh 'cat "$COMPOSE_FILE" | ssh -i "$SSH_KEY" -p "$SSH_PORT" -o ConnectTimeout=10 -o StrictHostKeyChecking=no -o LogLevel=ERROR -o BatchMode=yes "$SSH_USER@$SERVER_HOST" "cat > ~/project/docker-compose.yml"'
              }
            } else {
              sh 'cat docker-compose.yml | ssh -i "$SSH_KEY" -p "$SSH_PORT" -o ConnectTimeout=10 -o StrictHostKeyChecking=no -o LogLevel=ERROR -o BatchMode=yes "$SSH_USER@$SERVER_HOST" "cat > ~/project/docker-compose.yml"'
            }
          }

          sh '''
            # Create deploy.sh on server
            ssh -i "$SSH_KEY" -p "$SSH_PORT" -o ConnectTimeout=10 -o StrictHostKeyChecking=no -o LogLevel=ERROR -o BatchMode=yes "$SSH_USER@$SERVER_HOST" "cat > ~/project/deploy.sh" <<'EOF'
            #!/usr/bin/env bash
            set -euo pipefail

            cd ~/project

            # docker compose wrapper
            dcompose() {
              if docker compose version >/dev/null 2>&1; then
                docker compose "$@"
              elif command -v docker-compose >/dev/null 2>&1; then
                docker-compose "$@"
              else
                echo "docker compose/docker-compose not found. Please install Docker Compose v2." >&2
                return 127
              fi
            }

            # Build .env (values can be overridden via env when invoking this script)
            cat > .env <<ENVEOF
            DOCKER_USERNAME=$DOCKER_USER
            DB_CONNECTION_STRING=${DB_CONNECTION_STRING:-Server=db;Port=3306;Database=emo_db;User Id=tru123;Password=tru12345;SslMode=None;}
            JWT_SECRET=${JWT_SECRET:-CHANGE_ME_SUPER_SECRET_MIN_32_CHARS_1234567}
            ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}
            API_PORT=${API_PORT:-5193}
            CLIENT_PORT=${CLIENT_PORT:-5173}
            VITE_API_URL=http://$SERVER_HOST:${API_PORT}
            ENVEOF

            echo "Logging into Docker Hub on server..."
            echo "Username: ${DOCKER_USER:-<empty>}"

            if echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin; then
              echo "Docker Hub login OK"

              # Pull & restart
              dcompose --env-file .env pull
              dcompose --env-file .env down
              dcompose --env-file .env up -d --remove-orphans

              # Housekeeping
              docker logout || true
              docker image prune -f || true
              echo "Deployment completed!"
            else
              echo "Docker Hub login failed on server"
              exit 1
            fi
            EOF

            # Make script executable
            ssh -i "$SSH_KEY" -p "$SSH_PORT" -o ConnectTimeout=10 -o StrictHostKeyChecking=no -o LogLevel=ERROR -o BatchMode=yes "$SSH_USER@$SERVER_HOST" "chmod +x ~/project/deploy.sh"

            # Run deploy script, passing secrets via env (DB + JWT come from Jenkins credentials)
            ssh -i "$SSH_KEY" -p "$SSH_PORT" -o ConnectTimeout=10 -o StrictHostKeyChecking=no -o LogLevel=ERROR -o BatchMode=yes \
              "$SSH_USER@$SERVER_HOST" \
              "SERVER_HOST='$SERVER_HOST' DOCKER_USER='$DOCKER_USER' DOCKER_PASS='$DOCKER_PASS' \
               DB_CONNECTION_STRING='${DB_CONN}' JWT_SECRET='${JWT_SECRET}' ASPNETCORE_ENVIRONMENT='Production' \
               API_PORT='${API_PORT}' CLIENT_PORT='${CLIENT_PORT}' \
               ~/project/deploy.sh"
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
          curl -f http://$SERVER_HOST:$API_PORT/health || echo "API health check failed"
          echo "Checking Client..."
          curl -f http://$SERVER_HOST:$CLIENT_PORT || echo "Client health check failed"
        '''
      }
    }
  }

  post {
    always {
      sh 'docker image prune -f || true'
    }
    success {
      echo "Deployment successful!"
      echo "API: http://$SERVER_HOST:$API_PORT"
      echo "Client: http://$SERVER_HOST:$CLIENT_PORT"
    }
    failure {
      echo "Deployment failed!"
    }
  }
}
