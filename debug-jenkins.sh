#!/bin/bash

# Jenkins Pipeline Debug Script
# This script helps debug Jenkins pipeline issues

echo "🐛 Jenkins Pipeline Debug Tool"
echo "============================="

# Check if we're in the right directory
if [ ! -f "Jenkinsfile" ]; then
    echo "❌ Jenkinsfile not found in current directory"
    echo "   Please run this script from the project root directory"
    exit 1
fi

echo "✅ Jenkinsfile found"
echo ""

# Check Jenkinsfile content
echo "📋 Checking Jenkinsfile content..."
echo "=================================="

# Check if Docker commands are present
if grep -q "docker run" Jenkinsfile; then
    echo "✅ Docker commands found in Jenkinsfile"
else
    echo "❌ Docker commands NOT found in Jenkinsfile"
    echo "   The Jenkinsfile may not be updated correctly"
fi

# Check for old dotnet commands
if grep -q "dotnet test --no-restore" Jenkinsfile; then
    echo "❌ Old dotnet commands still present"
    echo "   This indicates Jenkinsfile is not updated"
else
    echo "✅ Old dotnet commands removed"
fi

echo ""
echo "🔍 Current Jenkinsfile Backend Tests section:"
echo "=============================================="
grep -A 20 "stage('Backend Tests')" Jenkinsfile | head -25

echo ""
echo "📊 Git Status:"
echo "=============="
git status

echo ""
echo "📝 Recent commits:"
echo "=================="
git log --oneline -5

echo ""
echo "🔧 Debugging Steps:"
echo "==================="
echo ""
echo "1. Verify Jenkinsfile is committed:"
echo "   git add Jenkinsfile"
echo "   git commit -m 'Fix dotnet not found error'"
echo "   git push origin main"
echo ""
echo "2. Check Jenkins job configuration:"
echo "   - Go to Jenkins Dashboard"
echo "   - Click on your job"
echo "   - Click 'Configure'"
echo "   - Verify 'Script Path' is set to 'Jenkinsfile'"
echo "   - Click 'Save'"
echo ""
echo "3. Force Jenkins to pull latest code:"
echo "   - In Jenkins job, click 'Build Now'"
echo "   - Or enable 'Poll SCM' trigger"
echo ""
echo "4. Check build logs:"
echo "   - Look for 'Running .NET tests using Docker...' message"
echo "   - Should NOT see 'dotnet: not found' error"
echo ""
echo "5. If still getting old error:"
echo "   - Jenkins may be using cached version"
echo "   - Try restarting Jenkins"
echo "   - Or delete and recreate the job"
echo ""
echo "🎯 Expected behavior after fix:"
echo "================================"
echo "✅ Should see: 'Running .NET tests using Docker...'"
echo "✅ Should see: 'Starting .NET tests...'"
echo "✅ Should see: 'Restoring packages...'"
echo "✅ Should see: 'Running tests...'"
echo "❌ Should NOT see: 'dotnet: not found'"
echo ""
echo "✨ Debug complete! Check the steps above to resolve the issue."
