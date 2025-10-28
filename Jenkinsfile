pipeline {
  agent any

  environment {
    REGISTRY   = "docker.io/${DOCKER_USER}"   // dùng cùng biến với creds
    IMAGE_NAME = "server-lms-net"
    SERVER_HOST = "47.128.79.251"
    SERVER_USER = "root"
  }

  stages {
    stage('Checkout') {
      steps {
        checkout([$class: 'GitSCM',
          branches: [[name: '*/main']],
          userRemoteConfigs: [[
            url: 'https://github.com/levanvutru358/Devops.git',
            credentialsId: 'github-pat'
          ]]
        ])
        sh 'pwd && ls -la && test -f EmoApp.sln && echo "✅ Found EmoApp.sln"'
      }
    }

    stage('Docker Build') {
      steps {
        withCredentials([usernamePassword(credentialsId: 'dockerhub-cred',
          usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {

          sh '''
            echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin
            # Build từ GỐC repo, chỉ định Dockerfile trong server/
            docker build -t docker.io/$DOCKER_USER/$IMAGE_NAME:latest -f server/Dockerfile .
          '''
        }
      }
    }

    stage('Push Docker Hub') {
      steps {
        withCredentials([usernamePassword(credentialsId: 'dockerhub-cred',
          usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {

          sh '''
            echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin
            docker push docker.io/$DOCKER_USER/$IMAGE_NAME:latest
            docker logout
          '''
        }
      }
    }

    stage('Deploy Server') {
      steps {
        withCredentials([
          usernamePassword(credentialsId: 'dockerhub-cred',
            usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS'),
          string(credentialsId: 'db-conn', variable: 'DB_CONN'),
          file(credentialsId: 'docker-compose-file', variable: 'DOCKER_COMPOSE_PATH')
        ]) {
          sshagent (credentials: ['server-ssh-key']) {
            sh '''
              # copy compose file lên server
              scp -o StrictHostKeyChecking=no "$DOCKER_COMPOSE_PATH" $SERVER_USER@$SERVER_HOST:~/project/docker-compose.yml

              # deploy
              ssh -o StrictHostKeyChecking=no $SERVER_USER@$SERVER_HOST "
                cd ~/project && \
                echo \\"DB_CONNECTION_STRING=$DB_CONN\\" > .env && \
                echo \\"$DOCKER_PASS\\" | docker login -u $DOCKER_USER --password-stdin && \
                docker compose --env-file .env pull && \
                docker compose --env-file .env down && \
                docker compose --env-file .env up -d && \
                docker image prune -f
              "
            '''
          }
        }
      }
    }
  }
}
