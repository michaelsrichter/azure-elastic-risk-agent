#!/bin/bash
set -e

# Deploy Functions using ZipDeploy without SAS tokens
# This script is a workaround for organizations that block SAS token access

echo "==================================="
echo "Deploying Function App without SAS"
echo "==================================="

# Get the function app name and resource group from azd environment
FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME 2>/dev/null || echo "")
RESOURCE_GROUP_NAME=$(azd env get-value AZURE_RESOURCE_GROUP 2>/dev/null || echo "")

if [ -z "$FUNCTION_APP_NAME" ]; then
    echo "ERROR: AZURE_FUNCTION_APP_NAME not found in azd environment"
    echo "Please run 'azd provision' first"
    exit 1
fi

if [ -z "$RESOURCE_GROUP_NAME" ]; then
    echo "ERROR: AZURE_RESOURCE_GROUP not found in azd environment"
    echo "Please run 'azd provision' first"
    exit 1
fi

echo "Function App: $FUNCTION_APP_NAME"
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo ""

# Build the project
echo "Building project..."
cd src/ElasticOn.RiskAgent.Demo.Functions
dotnet publish -c Release -o ./bin/publish

# Create zip file
echo "Creating deployment package..."
cd ./bin/publish
zip -r ../deploy.zip . > /dev/null
cd ../..

# Deploy using Azure CLI zip deploy API directly (bypasses SCM checks)
echo "Deploying to Azure Functions using ZipDeploy API..."
echo "This may take a few minutes..."

# Get the publishing credentials
echo "Getting deployment credentials..."
CREDS=$(az functionapp deployment list-publishing-credentials \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$FUNCTION_APP_NAME" \
    --query "{username:publishingUserName, password:publishingPassword}" \
    -o json)

USERNAME=$(echo "$CREDS" | jq -r .username)
PASSWORD=$(echo "$CREDS" | jq -r .password)

# Upload the zip file using curl with basic auth
echo "Uploading deployment package..."
curl -X POST \
    -u "$USERNAME:$PASSWORD" \
    --data-binary @"./bin/deploy.zip" \
    "https://${FUNCTION_APP_NAME}.scm.azurewebsites.net/api/zipdeploy" \
    --progress-bar \
    --retry 3 \
    --retry-delay 5

if [ $? -eq 0 ]; then
    echo ""
    echo "Deployment upload completed. Waiting for functions to start..."
    sleep 10
else
    echo ""
    echo "❌ Deployment failed during upload"
    exit 1
fi

echo ""
echo "✅ Deployment completed successfully!"
echo ""

# Clean up
rm -f ./bin/deploy.zip

cd ../..
