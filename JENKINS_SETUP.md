# Jenkins CI/CD Pipeline cho EmoApp

## Tổng quan

Pipeline Jenkins này được thiết kế cho dự án EmoApp full-stack bao gồm:
- **Backend**: .NET 9 Web API
- **Frontend**: React + Vite
- **Database**: MySQL
- **Containerization**: Docker + Docker Compose

## Cấu trúc Pipeline

### 1. Checkout
- Clone code từ GitHub repository
- Lấy thông tin commit hash

### 2. Backend Tests
- Chạy unit tests cho .NET API
- Publish test results

### 3. Frontend Tests
- Install dependencies với npm ci
- Chạy tests và coverage
- Publish coverage report

### 4. Build API Image
- Build Docker image cho backend
- Tag với build number và latest

### 5. Build Client Image
- Build Docker image cho frontend
- Set VITE_API_URL environment variable

### 6. Push Images to Docker Hub
- Push cả API và Client images
- Tag với build number và latest

### 7. Deploy to Server
- Copy docker-compose.yml lên server
- Tạo .env file với environment variables
- Deploy services với Docker Compose

### 8. Health Check
- Kiểm tra API health endpoint
- Kiểm tra Client accessibility

## Cấu hình Jenkins Credentials

Cần tạo các credentials sau trong Jenkins:

### 1. GitHub Personal Access Token
- **ID**: `github-pat`
- **Type**: Secret text
- **Description**: GitHub PAT for repository access

### 2. Docker Hub Credentials
- **ID**: `dockerhub-cred`
- **Type**: Username with password
- **Description**: Docker Hub login credentials

### 3. Database Connection String
- **ID**: `db-conn`
- **Type**: Secret text
- **Description**: MySQL connection string

### 4. JWT Secret
- **ID**: `jwt-secret`
- **Type**: Secret text
- **Description**: JWT signing secret

### 5. Server SSH Key
- **ID**: `server-ssh-key`
- **Type**: SSH Username with private key
- **Description**: SSH key for server deployment

## Environment Variables

Pipeline sử dụng các environment variables sau:

```bash
# Docker Registry
REGISTRY=docker.io

# Image Names
API_IMAGE_NAME=webshop-api
CLIENT_IMAGE_NAME=webshop-client

# Server Configuration
SERVER_HOST=47.128.79.251
SERVER_USER=root

# Build Information
BUILD_NUMBER=${env.BUILD_NUMBER}
GIT_COMMIT_SHORT=${env.GIT_COMMIT.take(7)}
```

## Server Requirements

### 1. Docker và Docker Compose
```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### 2. Directory Structure
```bash
mkdir -p ~/project
# docker-compose.yml sẽ được copy vào đây
```

### 3. Ports
- **5193**: Backend API
- **5173**: Frontend Client

## Cách sử dụng

### 1. Tạo Jenkins Job
1. Tạo new Pipeline job
2. Configure pipeline script from SCM
3. Chọn Git và nhập repository URL
4. Set branch to `main`
5. Set script path to `Jenkinsfile`

### 2. Chạy Pipeline
1. Click "Build Now" để chạy pipeline
2. Monitor progress trong console output
3. Check build status và logs

### 3. Access Application
Sau khi deploy thành công:
- **API**: http://47.128.79.251:5193
- **Client**: http://47.128.79.251:5173
- **API Health**: http://47.128.79.251:5193/health

## Troubleshooting

### 1. Build Failures
- Check Docker Hub credentials
- Verify server SSH access
- Check database connection string

### 2. Deployment Issues
- Verify server has Docker và Docker Compose
- Check server ports are available
- Verify environment variables

### 3. Health Check Failures
- Wait for services to fully start (30s)
- Check container logs: `docker logs emo-backend-api`
- Verify network connectivity

## Monitoring

Pipeline cung cấp:
- ✅ Test results và coverage reports
- ✅ Build status notifications
- ✅ Health check validation
- ✅ Detailed deployment logs

## Security Notes

- Tất cả credentials được lưu trữ an toàn trong Jenkins
- SSH keys sử dụng cho server access
- Environment variables được inject tại runtime
- Docker images được tag với build numbers để tracking
