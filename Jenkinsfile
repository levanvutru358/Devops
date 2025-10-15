pipeline {
  agent any
  options {
    timestamps()
    ansiColor('xterm')
    buildDiscarder(logRotator(numToKeepStr: '20'))
  }

  environment {
    IMAGE_NAME   = "server-lms-net"         // tên image cho API; FE dùng tên khác bên dưới
    IMAGE_NAME_FE = "webshop-fe"
    SERVER_HOST  = "47.128.79.251"
    SERVER_USER  = "root"

    // Nếu bạn có Dockerfile ở subfolder, set ở đây:
    // ví dụ: DOCKERFILE = 'backend/Dockerfile'; BUILD_CONTEXT = 'backend'
    DOCKERFILE   = ''        // để trống = auto detect/generate
    BUILD_CONTEXT = ''
  }

  stages {
    stage('Checkout') {
      steps {
        deleteDir()
        checkout([$class: 'GitSCM',
          branches: [[name: '*/main']],
          userRemoteConfigs: [[
            url: 'https://github.com/levanvutru358/Devops.git',
            credentialsId: 'github-pat'
          ]]
        ])
        sh 'echo "Workspace: $(pwd)"; ls -la'
      }
    }

    stage('Detect project') {
      steps {
        script {
          // Tự phát hiện
          env.CS_PROJ = sh(script: 'ls -1 **/*.csproj 2>/dev/null | head -n1 || true', returnStdout: true).trim()
          env.HAS_FE  = sh(script: 'test -f package.json && echo yes || echo no', returnStdout: true).trim()
          echo "Detected -> CS_PROJ=${env.CS_PROJ}, HAS_FE=${env.HAS_FE}"
        }
      }
    }

    stage('Docker Build & Push') {
      steps {
        withCredentials([usernamePassword(credentialsId: 'dockerhub-cred',
                            usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
          script {
            sh 'echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin'

            // --- Nếu chỉ định sẵn DOCKERFILE & BUILD_CONTEXT ---
            if (env.DOCKERFILE?.trim()) {
              sh """
                set -eux
                docker build -t docker.io/${'$'}DOCKER_USER/${IMAGE_NAME}:latest \
                  -f "${DOCKERFILE}" "${BUILD_CONTEXT:-.}"
              """
              sh "docker push docker.io/${'$'}DOCKER_USER/${IMAGE_NAME}:latest"
              return
            }

            // --- Auto build FE nếu có package.json ---
            if (env.HAS_FE == 'yes') {
              sh '''
                set -eux
                cat > Dockerfile <<'EOF'
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx","-g","daemon off;"]
EOF
                docker build -t docker.io/$DOCKER_USER/'${IMAGE_NAME_FE}':latest .
                docker push docker.io/$DOCKER_USER/'${IMAGE_NAME_FE}':latest
              '''
            }

            // --- Auto build .NET nếu tìm thấy csproj ---
            if (env.CS_PROJ) {
              sh """
                set -eux
                # Tạo Dockerfile .NET khớp đường dẫn csproj đã phát hiện
                cat > Dockerfile <<EOF
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ${env.CS_PROJ} app.csproj
RUN dotnet restore app.csproj
COPY . .
RUN dotnet publish app.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet","$(basename "${env.CS_PROJ}".csproj | sed 's/.csproj$/.dll/')"]
EOF

                docker build -t docker.io/$DOCKER_USER/${IMAGE_NAME}:latest .
                docker push docker.io/$DOCKER_USER/${IMAGE_NAME}:latest
              """
            }

            // Không có FE cũng không có .NET?
            if (env.HAS_FE != 'yes' && !env.CS_PROJ) {
              error "Không thấy Dockerfile, package.json hay *.csproj trong repo. Hãy đặt Dockerfile đúng chỗ hoặc set DOCKERFILE/BUILD_CONTEXT."
            }
          }
        }
      }
    }

    stage('Deploy Server') {
      steps {
        withCredentials([
          usernamePassword(credentialsId: 'dockerhub-cred', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS'),
          string(credentialsId: 'db-conn', variable: 'DB_CONN'),
          file(credentialsId: 'docker-compose-file', variable: 'DOCKER_COMPOSE_PATH')
        ]) {
          sshagent (credentials: ['server-ssh-key']) {
            sh '''
              set -eux
              # Upload docker-compose.yml
              ssh -o StrictHostKeyChecking=no $SERVER_USER@$SERVER_HOST 'mkdir -p ~/project'
              scp -o StrictHostKeyChecking=no "$DOCKER_COMPOSE_PATH" $SERVER_USER@$SERVER_HOST:~/project/docker-compose.yml

              # Deploy
              ssh -o StrictHostKeyChecking=no $SERVER_USER@$SERVER_HOST '
                set -eux
                cd ~/project
                echo "DB_CONNECTION_STRING=$DB_CONN" > .env
                echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin || true

                # Ưu tiên docker compose plugin; fallback sang docker-compose
                if docker compose version >/dev/null 2>&1; then
                  docker compose --env-file .env pull
                  docker compose --env-file .env down
                  docker compose --env-file .env up -d
                else
                  docker-compose --env-file .env pull
                  docker-compose --env-file .env down
                  docker-compose --env-file .env up -d
                fi

                docker image prune -f || true
              '
            '''
          }
        }
      }
    }
  }

  post {
    always {
      sh 'docker logout || true'
      cleanWs()
    }
  }
}
