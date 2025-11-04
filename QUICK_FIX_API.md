# 🔍 Check API Container Logs - Quick Commands

## Run these commands on your server:

```bash
# Check container logs
docker logs emo-backend-api --tail 50

# Check what files are in the container
docker exec emo-backend-api ls -la /app

# Check if EmoApi.dll exists
docker exec emo Ad.* ls -la /app/EmoApi.dll

# Check what DLLs exist
docker exec emo-backend-api ls -la /app/*.dll

# Check if the new image was pulled
docker images | grep webshop-api
```

## 🔧 Quick Fix:

The issue is that the Docker image on Docker Hub still has the old Dockerfile. 
You need to rebuild and push a new image.

### Option 1: Re-run Jenkins Pipeline (Recommended)
1. Go to Jenkins Dashboard
2. Run job "EmoApp-CI-CD" again
3. This will build a new image with the fixed Dockerfile

### Option 2: Manual Rebuild on Server (Quick Fix)
```bash
cd ~/project

# Pull the new image that will be built by Jenkins
# OR build locally if you have the code
docker compose down

# After Jenkins builds the new image, run:
docker compose pull
docker compose up -d
```

## 🎯 The Fix:
- The Dockerfile now uses `CMD ["dotnet", "EmoApi.dll"]` instead of dynamic lookup
- But the image on Docker Hub still has the old code
- Need to rebuild the image with the new Dockerfile
