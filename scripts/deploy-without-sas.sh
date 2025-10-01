#!/bin/bash
set -e

# Deploy Functions using ZipDeploy without SAS tokens
# This script is a workaround for organizations that block SAS token access
# For Flex Consumption plans with managed identity storage

echo "==================================="
echo "Deploying Function App without SAS"
echo "==================================="

# Get environment variables from azd
FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME 2>/dev/null || echo "")
RESOURCE_GROUP_NAME=$(azd env get-value AZURE_RESOURCE_GROUP 2>/dev/null || echo "")
STORAGE_ACCOUNT_NAME=$(azd env get-value AZURE_STORAGE_ACCOUNT_NAME 2>/dev/null || echo "")

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

if [ -z "$STORAGE_ACCOUNT_NAME" ]; then
    echo "ERROR: AZURE_STORAGE_ACCOUNT_NAME not found in azd environment"
    echo "Please run 'azd provision' first"
    exit 1
fi

echo "Function App: $FUNCTION_APP_NAME"
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo "Storage Account: $STORAGE_ACCOUNT_NAME"
echo ""

# Temporarily enable public network access on storage account
echo "Temporarily enabling public network access on storage account..."
ORIGINAL_PUBLIC_ACCESS=$(az storage account show \
    --name "$STORAGE_ACCOUNT_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --query "publicNetworkAccess" -o tsv)

echo "Original public network access setting: $ORIGINAL_PUBLIC_ACCESS"

az storage account update \
    --name "$STORAGE_ACCOUNT_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --public-network-access Enabled \
    --output none

echo "Public network access enabled temporarily"
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

# Upload to a blob container in the storage account using Azure CLI
echo "Uploading deployment package to storage account..."
BLOB_NAME="deployments/deploy-$(date +%s).zip"

# Get the current user's object ID for blob upload
USER_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

# Grant the user Storage Blob Data Contributor role if needed
echo "Ensuring you have permissions to upload to storage..."
az role assignment create \
    --role "Storage Blob Data Contributor" \
    --assignee "$USER_OBJECT_ID" \
    --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP_NAME/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT_NAME" \
    --output none 2>/dev/null || true

# Wait for permissions to propagate
sleep 3

# Create container if it doesn't exist
az storage container create \
    --name deployments \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --auth-mode login \
    --only-show-errors \
    --output none 2>/dev/null || true

# Upload the zip file
echo "Uploading to blob storage..."
az storage blob upload \
    --container-name deployments \
    --name "$BLOB_NAME" \
    --file "./bin/deploy.zip" \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --auth-mode login \
    --overwrite \
    --only-show-errors

# Get the blob URL (no SAS token needed - function app will use managed identity)
BLOB_URL="https://${STORAGE_ACCOUNT_NAME}.blob.core.windows.net/deployments/${BLOB_NAME}"

echo "✅ Blob uploaded to: $BLOB_URL"
echo ""

# Restore original public network access setting
echo "Restoring original public network access setting..."
if [ "$ORIGINAL_PUBLIC_ACCESS" = "Disabled" ]; then
    az storage account update \
        --name "$STORAGE_ACCOUNT_NAME" \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --public-network-access Disabled \
        --output none
    echo "Public network access restored to: Disabled"
elif [ "$ORIGINAL_PUBLIC_ACCESS" = "Enabled" ]; then
    echo "Public network access was already Enabled, no change needed"
else
    echo "Note: Original setting was '$ORIGINAL_PUBLIC_ACCESS'"
fi
echo ""

# Set the WEBSITE_RUN_FROM_PACKAGE setting to point to the blob
# The function app will use its managed identity to access the blob
echo "Configuring function app to run from package..."
az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --settings "WEBSITE_RUN_FROM_PACKAGE=${BLOB_URL}" \
    --output none

echo "Restarting function app to apply changes..."
az functionapp restart \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --output none

echo ""
echo "Waiting for function app to start..."
sleep 20

# Verify deployment
echo "Verifying deployment..."
HEALTH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://${FUNCTION_APP_NAME}.azurewebsites.net/api/health" 2>/dev/null || echo "000")

if [ "$HEALTH_STATUS" = "200" ]; then
    echo "✅ Deployment completed successfully!"
    echo "   Health check passed (HTTP $HEALTH_STATUS)"
    echo ""
    echo "Your functions are now running from: $BLOB_URL"
else
    echo "⚠️  Deployment completed but health check returned HTTP $HEALTH_STATUS"
    echo "   The app may still be starting up. Wait a minute and try:"
    echo "   curl https://${FUNCTION_APP_NAME}.azurewebsites.net/api/health"
    echo ""
    echo "If issues persist, check the logs:"
    echo "   az webapp log tail --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP_NAME"
fi

echo ""

# Clean up
rm -f ./bin/deploy.zip

cd ../..
