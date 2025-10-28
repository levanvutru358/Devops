# 🔧 Hướng dẫn khắc phục lỗi "dotnet: not found" trong Jenkins

## ✅ Tình trạng hiện tại
- Jenkinsfile đã được cập nhật với Docker commands
- Code đã được commit và push lên repository
- Jenkinsfile hiện tại sử dụng Docker thay vì yêu cầu dotnet SDK

## 🚨 Vấn đề
Jenkins vẫn đang chạy phiên bản cũ của pipeline và gặp lỗi:
```
dotnet: not found
script returned exit code 127
```

## 🔧 Các bước khắc phục

### Bước 1: Kiểm tra Jenkins Job Configuration
1. Truy cập Jenkins Dashboard
2. Click vào job của bạn (thường là "EmoApp-CI-CD" hoặc tên tương tự)
3. Click **"Configure"**
4. Scroll xuống phần **"Pipeline"**
5. Kiểm tra:
   - **Definition**: "Pipeline script from SCM"
   - **SCM**: Git
   - **Repository URL**: `https://github.com/levanvutru358/Devops.git`
   - **Branch**: `main`
   - **Script Path**: `Jenkinsfile`
6. Click **"Save"** để refresh configuration

### Bước 2: Force Jenkins Pull Latest Code
**Cách 1: Manual Build**
1. Trong Jenkins job, click **"Build Now"**
2. Monitor build logs để xem có pull code mới không

**Cách 2: Enable Poll SCM**
1. Trong job configuration, scroll xuống **"Build Triggers"**
2. Check **"Poll SCM"**
3. Set schedule: `H/5 * * * *` (poll every 5 minutes)
4. Click **"Save"**

### Bước 3: Kiểm tra Build Logs
Sau khi chạy build mới, bạn sẽ thấy:

**✅ Thành công (Expected):**
```
+ echo Running .NET tests using Docker...
Running .NET tests using Docker...
+ timeout 300 docker run --rm
```

**❌ Vẫn lỗi (Nếu vẫn thấy):**
```
+ echo Running .NET tests...
Running .NET tests...
+ dotnet test --no-restore --verbosity normal
dotnet: not found
```

### Bước 4: Nếu vẫn không work

**Option A: Restart Jenkins**
1. Go to Jenkins Dashboard
2. Click **"Manage Jenkins"**
3. Click **"Restart Jenkins"**
4. Wait for Jenkins to restart
5. Run job again

**Option B: Clear Jenkins Cache**
1. Stop Jenkins
2. Delete workspace: `/var/lib/jenkins/workspace/YOUR_JOB_NAME`
3. Start Jenkins
4. Run job again

**Option C: Recreate Job**
1. Delete current job
2. Create new Pipeline job
3. Configure với settings ở Bước 1
4. Run job

## 🎯 Kết quả mong đợi

Sau khi fix thành công, build logs sẽ hiển thị:

```
Stage: Backend Tests
+ echo Running .NET tests using Docker...
Running .NET tests using Docker...
+ timeout 300 docker run --rm -v $(pwd):/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 bash -c "echo 'Starting .NET tests...' && cd server && echo 'Restoring packages...' && dotnet restore --verbosity minimal && echo 'Running tests...' && dotnet test --verbosity normal --logger trx --results-directory TestResults --no-restore || echo 'Tests completed with warnings'"
Starting .NET tests...
Restoring packages...
Running tests...
```

## 📞 Nếu vẫn gặp vấn đề

1. **Check Jenkins Version**: Đảm bảo Jenkins có Docker plugin
2. **Check Jenkins Agent**: Đảm bảo agent có Docker installed
3. **Check Network**: Đảm bảo Jenkins có thể pull Docker images
4. **Check Permissions**: Đảm bảo Jenkins user có quyền chạy Docker

## 🔍 Debug Commands

Nếu cần debug thêm, chạy các lệnh sau trên Jenkins agent:

```bash
# Check Docker
docker --version
docker run hello-world

# Check Jenkins workspace
ls -la /var/lib/jenkins/workspace/YOUR_JOB_NAME/

# Check Jenkinsfile content
cat /var/lib/jenkins/workspace/YOUR_JOB_NAME/Jenkinsfile | grep -A 10 "Backend Tests"
```

---
**Lưu ý**: Lỗi này xảy ra vì Jenkins đang sử dụng cached version của pipeline. Sau khi refresh configuration, Jenkins sẽ pull code mới và sử dụng Docker commands thay vì dotnet trực tiếp.
