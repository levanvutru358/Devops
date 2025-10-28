# Jenkins Pipeline Debug Script (PowerShell)
# This script helps debug Jenkins pipeline issues

Write-Host "🐛 Jenkins Pipeline Debug Tool" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Check if we're in the right directory
if (-not (Test-Path "Jenkinsfile")) {
    Write-Host "❌ Jenkinsfile not found in current directory" -ForegroundColor Red
    Write-Host "   Please run this script from the project root directory" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Jenkinsfile found" -ForegroundColor Green
Write-Host ""

# Check Jenkinsfile content
Write-Host "📋 Checking Jenkinsfile content..." -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

# Read Jenkinsfile content
$jenkinsfileContent = Get-Content "Jenkinsfile" -Raw

# Check if Docker commands are present
if ($jenkinsfileContent -match "docker run") {
    Write-Host "✅ Docker commands found in Jenkinsfile" -ForegroundColor Green
} else {
    Write-Host "❌ Docker commands NOT found in Jenkinsfile" -ForegroundColor Red
    Write-Host "   The Jenkinsfile may not be updated correctly" -ForegroundColor Yellow
}

# Check for old dotnet commands
if ($jenkinsfileContent -match "dotnet test --no-restore") {
    Write-Host "❌ Old dotnet commands still present" -ForegroundColor Red
    Write-Host "   This indicates Jenkinsfile is not updated" -ForegroundColor Yellow
} else {
    Write-Host "✅ Old dotnet commands removed" -ForegroundColor Green
}

Write-Host ""
Write-Host "🔍 Current Jenkinsfile Backend Tests section:" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# Extract Backend Tests section
$lines = Get-Content "Jenkinsfile"
$inBackendTests = $false
$backendTestsLines = @()

foreach ($line in $lines) {
    if ($line -match "stage\('Backend Tests'\)") {
        $inBackendTests = $true
    }
    if ($inBackendTests) {
        $backendTestsLines += $line
        if ($line -match "^\s*}\s*$" -and $backendTestsLines.Count -gt 5) {
            break
        }
    }
}

$backendTestsLines | Select-Object -First 25 | ForEach-Object { Write-Host $_ }

Write-Host ""
Write-Host "📊 Git Status:" -ForegroundColor Cyan
Write-Host "==============" -ForegroundColor Cyan
git status

Write-Host ""
Write-Host "📝 Recent commits:" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
git log --oneline -5

Write-Host ""
Write-Host "🔧 Debugging Steps:" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Verify Jenkinsfile is committed:" -ForegroundColor Yellow
Write-Host "   git add Jenkinsfile" -ForegroundColor White
Write-Host "   git commit -m 'Fix dotnet not found error'" -ForegroundColor White
Write-Host "   git push origin main" -ForegroundColor White
Write-Host ""
Write-Host "2. Check Jenkins job configuration:" -ForegroundColor Yellow
Write-Host "   - Go to Jenkins Dashboard" -ForegroundColor White
Write-Host "   - Click on your job" -ForegroundColor White
Write-Host "   - Click 'Configure'" -ForegroundColor White
Write-Host "   - Verify 'Script Path' is set to 'Jenkinsfile'" -ForegroundColor White
Write-Host "   - Click 'Save'" -ForegroundColor White
Write-Host ""
Write-Host "3. Force Jenkins to pull latest code:" -ForegroundColor Yellow
Write-Host "   - In Jenkins job, click 'Build Now'" -ForegroundColor White
Write-Host "   - Or enable 'Poll SCM' trigger" -ForegroundColor White
Write-Host ""
Write-Host "4. Check build logs:" -ForegroundColor Yellow
Write-Host "   - Look for 'Running .NET tests using Docker...' message" -ForegroundColor White
Write-Host "   - Should NOT see 'dotnet: not found' error" -ForegroundColor White
Write-Host ""
Write-Host "5. If still getting old error:" -ForegroundColor Yellow
Write-Host "   - Jenkins may be using cached version" -ForegroundColor White
Write-Host "   - Try restarting Jenkins" -ForegroundColor White
Write-Host "   - Or delete and recreate the job" -ForegroundColor White
Write-Host ""
Write-Host "🎯 Expected behavior after fix:" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "✅ Should see: 'Running .NET tests using Docker...'" -ForegroundColor Green
Write-Host "✅ Should see: 'Starting .NET tests...'" -ForegroundColor Green
Write-Host "✅ Should see: 'Restoring packages...'" -ForegroundColor Green
Write-Host "✅ Should see: 'Running tests...'" -ForegroundColor Green
Write-Host "❌ Should NOT see: 'dotnet: not found'" -ForegroundColor Red
Write-Host ""
Write-Host "Debug complete! Check the steps above to resolve the issue." -ForegroundColor Cyan
