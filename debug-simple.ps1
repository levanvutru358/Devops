# Jenkins Pipeline Debug Script (PowerShell)
Write-Host "Jenkins Pipeline Debug Tool" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

# Check if Jenkinsfile exists
if (-not (Test-Path "Jenkinsfile")) {
    Write-Host "Jenkinsfile not found in current directory" -ForegroundColor Red
    exit 1
}

Write-Host "Jenkinsfile found" -ForegroundColor Green
Write-Host ""

# Check Jenkinsfile content
Write-Host "Checking Jenkinsfile content..." -ForegroundColor Cyan
$jenkinsfileContent = Get-Content "Jenkinsfile" -Raw

# Check if Docker commands are present
if ($jenkinsfileContent -match "docker run") {
    Write-Host "Docker commands found in Jenkinsfile" -ForegroundColor Green
} else {
    Write-Host "Docker commands NOT found in Jenkinsfile" -ForegroundColor Red
}

# Check for old dotnet commands
if ($jenkinsfileContent -match "dotnet test --no-restore") {
    Write-Host "Old dotnet commands still present" -ForegroundColor Red
} else {
    Write-Host "Old dotnet commands removed" -ForegroundColor Green
}

Write-Host ""
Write-Host "Git Status:" -ForegroundColor Cyan
git status

Write-Host ""
Write-Host "Recent commits:" -ForegroundColor Cyan
git log --oneline -5

Write-Host ""
Write-Host "Debugging Steps:" -ForegroundColor Cyan
Write-Host "1. Commit Jenkinsfile changes:" -ForegroundColor Yellow
Write-Host "   git add Jenkinsfile" -ForegroundColor White
Write-Host "   git commit -m 'Fix dotnet not found error'" -ForegroundColor White
Write-Host "   git push origin main" -ForegroundColor White
Write-Host ""
Write-Host "2. Refresh Jenkins job configuration" -ForegroundColor Yellow
Write-Host "3. Run the job again" -ForegroundColor Yellow
Write-Host ""
Write-Host "Expected: Should see 'Running .NET tests using Docker...'" -ForegroundColor Green
Write-Host "Should NOT see: 'dotnet: not found'" -ForegroundColor Red
