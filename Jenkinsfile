pipeline {
    agent any

    environment {
        REGISTRY = "docker.io/${DOCKER_USERNAME}"
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
            }
        }

        // stage('Build & Test') {
        //     steps {
        //         sh 'dotnet restore LearnKing.sln'
        //         sh 'dotnet build LearnKing.sln -c Release'
        //         //sh 'dotnet test LearnKing.sln'
        //     }
        // }

        stage('Docker Build') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'dockerhub-cred',
                    usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                    
                    sh "docker build -t docker.io/$DOCKER_USER/$IMAGE_NAME:latest ."
                }
            }
        }

        stage('Push Docker Hub') {
            steps {
               withCredentials([usernamePassword(credentialsId: 'dockerhub-cred',
                    usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
                    
                    sh "echo $DOCKER_PASS | docker login -u $DOCKER_USER --password-stdin"
                    sh "docker push docker.io/$DOCKER_USER/$IMAGE_NAME:latest"
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
                        # Copy docker-compose.yml từ Jenkins sang server
                        scp -o StrictHostKeyChecking=no $DOCKER_COMPOSE_PATH $SERVER_USER@$SERVER_HOST:~/project/docker-compose.yml

                        # SSH vào server để deploy
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
