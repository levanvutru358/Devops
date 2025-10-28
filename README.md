# Jenkins CI/CD cho EmoApp

## Cấu hình Jenkins

### 1. Tạo Credentials
- `dockerhub-cred`: Docker Hub username/password
- `server-ssh-key`: SSH key để deploy lên server

### 2. Tạo Pipeline Job
- **Name**: EmoApp-CI-CD
- **Type**: Pipeline
- **Definition**: Pipeline script from SCM
- **SCM**: Git
- **Repository**: https://github.com/levanvutru358/Devops.git
- **Branch**: main
- **Script Path**: Jenkinsfile

### 3. Chạy Pipeline
Click "Build Now" để chạy pipeline.

## Pipeline Stages

1. **Checkout**: Clone code từ GitHub
2. **Build & Test**: 
   - Backend: Test .NET API với Docker
   - Frontend: Build React app với Docker
3. **Build Images**: Tạo Docker images cho API và Client
4. **Push Images**: Push lên Docker Hub
5. **Deploy**: Deploy lên server với Docker Compose
6. **Health Check**: Kiểm tra services hoạt động

## Kết quả
- **API**: http://47.128.79.251:5193
- **Client**: http://47.128.79.251:5173

## Troubleshooting
- Nếu lỗi Docker: Kiểm tra Jenkins agent có Docker không
- Nếu lỗi SSH: Kiểm tra SSH key và server access
- Nếu lỗi credentials: Kiểm tra Docker Hub credentials
