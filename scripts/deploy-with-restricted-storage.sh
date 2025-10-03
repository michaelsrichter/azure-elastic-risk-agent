#!/bin/bash
set -e

# Deploy with Restricted Storage Access
# This script enables public network access on the storage account if needed,
# then runs azd deploy. Public access remains enabled after deployment.
# 
# Use this when your organization has policies that disable public access to storage accounts.

echo "====================================="
echo "Deploy with Restricted Storage Access"
echo "====================================="
echo ""

# Get the storage account name from azd environment
STORAGE_ACCOUNT_NAME=$(azd env get-value AZURE_STORAGE_ACCOUNT_NAME 2>/dev/null || echo "")

if [ -z "$STORAGE_ACCOUNT_NAME" ]; then
    echo "❌ Error: Could not find AZURE_STORAGE_ACCOUNT_NAME in azd environment"
    echo "   Please run 'azd provision' first or ensure the environment is properly configured"
    exit 1
fi

echo "Storage Account: $STORAGE_ACCOUNT_NAME"
echo ""

# Get current public network access setting
echo "Checking current storage account configuration..."
CURRENT_ACCESS=$(az storage account show \
    --name "$STORAGE_ACCOUNT_NAME" \
    --query "publicNetworkAccess" \
    --output tsv 2>/dev/null || echo "Unknown")

echo "Current public network access: $CURRENT_ACCESS"
echo ""

# Enable public network access temporarily
if [ "$CURRENT_ACCESS" != "Enabled" ]; then
    echo "Enabling public network access temporarily..."
    az storage account update \
        --name "$STORAGE_ACCOUNT_NAME" \
        --public-network-access Enabled \
        --output none
    
    if [ $? -eq 0 ]; then
        echo "✓ Public network access enabled"
    else
        echo "❌ Failed to enable public network access"
        exit 1
    fi
    
    # Wait a moment for the change to propagate
    echo "Waiting for configuration to propagate..."
    sleep 5
else
    echo "ℹ️  Public network access already enabled"
fi

echo ""
echo "Running azd deploy..."
echo "-----------------------------------"

# Run azd deploy
azd deploy

DEPLOY_EXIT_CODE=$?

echo "-----------------------------------"
echo ""

# Note: We do not disable public access after deployment
# Public access remains enabled for subsequent deployments and operations
echo "ℹ️  Public network access remains enabled for future deployments"

echo ""

# Check deployment result
if [ $DEPLOY_EXIT_CODE -eq 0 ]; then
    echo "✅ Deployment completed successfully"
    
    # Get function app name and show endpoint
    FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME 2>/dev/null || echo "")
    if [ -n "$FUNCTION_APP_NAME" ]; then
        echo "   Function App: https://${FUNCTION_APP_NAME}.azurewebsites.net"
    fi
    
    echo ""
    echo "🎉 Deployment complete!"
else
    echo "❌ Deployment failed with exit code: $DEPLOY_EXIT_CODE"
    exit $DEPLOY_EXIT_CODE
fi
