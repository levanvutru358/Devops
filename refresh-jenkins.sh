#!/bin/bash

# Jenkins Pipeline Refresh Script
# This script helps refresh Jenkins job to use the latest Jenkinsfile

echo "🔄 Refreshing Jenkins Pipeline for EmoApp"
echo "=========================================="

# Configuration
JENKINS_URL="http://localhost:8080"
JENKINS_USER="admin"
JOB_NAME="EmoApp-CI-CD"

echo "📋 Configuration:"
echo "   Jenkins URL: $JENKINS_URL"
echo "   Job Name: $JOB_NAME"
echo ""

# Check if Jenkins CLI is available
if ! command -v jenkins-cli &> /dev/null; then
    echo "❌ Jenkins CLI not found."
    echo "   Please download jenkins-cli.jar from: $JENKINS_URL/jnlpJars/jenkins-cli.jar"
    echo ""
    echo "📖 Manual Steps to Refresh Pipeline:"
    echo "1. Go to Jenkins Dashboard"
    echo "2. Click on your job: $JOB_NAME"
    echo "3. Click 'Configure'"
    echo "4. Scroll down to 'Pipeline' section"
    echo "5. Click 'Save' (this will refresh the pipeline definition)"
    echo "6. Run the job again"
    echo ""
    echo "🔍 Alternative: Check if Jenkins is pulling latest code:"
    echo "   - Verify SCM polling is enabled"
    echo "   - Check 'Build Triggers' section"
    echo "   - Ensure 'Poll SCM' is configured"
    exit 1
fi

echo "🔍 Checking Jenkins job status..."
java -jar jenkins-cli.jar -s $JENKINS_URL -auth $JENKINS_USER:$JENKINS_PASSWORD get-job $JOB_NAME > /tmp/job-config.xml

if [ $? -eq 0 ]; then
    echo "✅ Job found: $JOB_NAME"
    
    echo "🔄 Refreshing job configuration..."
    java -jar jenkins-cli.jar -s $JENKINS_URL -auth $JENKINS_USER:$JENKINS_PASSWORD reload-job $JOB_NAME
    
    if [ $? -eq 0 ]; then
        echo "✅ Job reloaded successfully!"
        echo ""
        echo "🚀 Next steps:"
        echo "1. Go to Jenkins Dashboard"
        echo "2. Click on job: $JOB_NAME"
        echo "3. Click 'Build Now'"
        echo "4. Monitor the build logs"
    else
        echo "❌ Failed to reload job"
        echo "   Please check Jenkins credentials and job name"
    fi
else
    echo "❌ Job not found: $JOB_NAME"
    echo "   Please create the job first using setup-jenkins.sh"
fi

echo ""
echo "🔧 Troubleshooting Tips:"
echo "========================="
echo ""
echo "1. Verify Jenkinsfile is committed to repository:"
echo "   git add Jenkinsfile"
echo "   git commit -m 'Update Jenkinsfile with Docker testing'"
echo "   git push origin main"
echo ""
echo "2. Check Jenkins SCM configuration:"
echo "   - Repository URL: https://github.com/levanvutru358/Devops.git"
echo "   - Branch: main"
echo "   - Script Path: Jenkinsfile"
echo ""
echo "3. Force Jenkins to pull latest code:"
echo "   - Go to job configuration"
echo "   - Click 'Save' to refresh"
echo "   - Or trigger 'Poll SCM' manually"
echo ""
echo "4. Check build logs for errors:"
echo "   - Look for 'dotnet: not found' errors"
echo "   - Verify Docker commands are being used"
echo ""
echo "✨ If issues persist, check the updated Jenkinsfile in repository!"
