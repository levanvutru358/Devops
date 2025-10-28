#!/bin/bash

# Jenkins Job Setup Script for EmoApp
# This script helps setup the Jenkins job configuration

echo "🚀 Setting up Jenkins Job for EmoApp CI/CD Pipeline"
echo "=================================================="

# Check if Jenkins CLI is available
if ! command -v jenkins-cli &> /dev/null; then
    echo "❌ Jenkins CLI not found. Please install Jenkins CLI first."
    echo "   Download from: http://your-jenkins-server/jnlpJars/jenkins-cli.jar"
    exit 1
fi

# Jenkins server configuration
JENKINS_URL="http://localhost:8080"
JENKINS_USER="admin"
JENKINS_PASSWORD=""

echo "📋 Jenkins Configuration:"
echo "   URL: $JENKINS_URL"
echo "   User: $JENKINS_USER"
echo ""

# Create job configuration
echo "📝 Creating Jenkins job configuration..."

cat > emoapp-job.xml << 'EOF'
<?xml version='1.0' encoding='UTF-8'?>
<flow-definition plugin="workflow-job@2.45">
  <description>Full-stack CI/CD pipeline for EmoApp (.NET + React)</description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <hudson.plugins.discard__build.DiscardBuildProperty plugin="discard@1.05">
      <strategy class="hudson.plugins.discard__build.DiscardOldBuildStrategy">
        <daysToKeepStr>7</daysToKeepStr>
        <numToKeepStr>10</numToKeepStr>
        <artifactDaysToKeepStr>-1</artifactDaysToKeepStr>
        <artifactNumToKeepStr>-1</artifactNumToKeepStr>
      </strategy>
    </hudson.plugins.discard__build.DiscardBuildProperty>
    <hudson.triggers.SCMTrigger>
      <spec>H/5 * * * *</spec>
      <ignorePostCommitHooks>false</ignorePostCommitHooks>
    </hudson.triggers.SCMTrigger>
  </properties>
  <definition class="org.jenkinsci.plugins.workflow.cps.CpsScmFlowDefinition" plugin="workflow-cps@2.92">
    <scm class="hudson.plugins.git.GitSCM" plugin="git@4.8.3">
      <configVersion>2</configVersion>
      <userRemoteConfigs>
        <hudson.plugins.git.UserRemoteConfig>
          <url>https://github.com/levanvutru358/Devops.git</url>
          <credentialsId>github-pat</credentialsId>
        </hudson.plugins.git.UserRemoteConfig>
      </userRemoteConfigs>
      <branches>
        <hudson.plugins.git.BranchSpec>
          <name>*/main</name>
        </hudson.plugins.git.BranchSpec>
      </branches>
      <doGenerateSubmoduleConfigurations>false</doGenerateSubmoduleConfigurations>
      <submoduleCfg class="list"/>
      <extensions/>
    </scm>
    <scriptPath>Jenkinsfile</scriptPath>
    <lightweight>false</lightweight>
  </definition>
  <triggers/>
  <disabled>false</disabled>
</flow-definition>
EOF

echo "✅ Job configuration created: emoapp-job.xml"
echo ""

# Instructions for manual setup
echo "📖 Manual Setup Instructions:"
echo "============================="
echo ""
echo "1. Install required Jenkins plugins:"
echo "   - Docker Pipeline Plugin"
echo "   - Docker Plugin"
echo "   - SSH Agent Plugin"
echo "   - Credentials Binding Plugin"
echo "   - HTML Publisher Plugin"
echo "   - JUnit Plugin"
echo ""
echo "2. Create credentials in Jenkins:"
echo "   - github-pat (Secret text): GitHub Personal Access Token"
echo "   - dockerhub-cred (Username with password): Docker Hub credentials"
echo "   - db-conn (Secret text): Database connection string"
echo "   - jwt-secret (Secret text): JWT signing secret"
echo "   - server-ssh-key (SSH Username with private key): SSH key for server"
echo ""
echo "3. Create new Pipeline job:"
echo "   - Go to Jenkins Dashboard"
echo "   - Click 'New Item'"
echo "   - Enter name: 'EmoApp-CI-CD'"
echo "   - Select 'Pipeline'"
echo "   - Click 'OK'"
echo ""
echo "4. Configure the job:"
echo "   - Pipeline definition: Pipeline script from SCM"
echo "   - SCM: Git"
echo "   - Repository URL: https://github.com/levanvutru358/Devops.git"
echo "   - Credentials: github-pat"
echo "   - Branch: main"
echo "   - Script Path: Jenkinsfile"
echo ""
echo "5. Save and run the job!"
echo ""
echo "🔧 Alternative: Use Jenkins CLI to create job automatically:"
echo "   java -jar jenkins-cli.jar -s $JENKINS_URL -auth $JENKINS_USER:$JENKINS_PASSWORD create-job EmoApp-CI-CD < emoapp-job.xml"
echo ""
echo "📚 For detailed instructions, see JENKINS_SETUP.md"
echo ""
echo "✨ Setup complete! Happy coding! 🎉"
