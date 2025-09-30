#!/bin/bash

# Azure Developer CLI Setup Script
# This script sets up the required environment for AZD deployment

set -e

echo "🚀 Setting up Azure Developer CLI environment..."

# Check if user is logged into Azure CLI
if ! az account show &> /dev/null; then
    echo "❌ You are not logged into Azure CLI. Please run 'az login' first."
    exit 1
fi

# Check if user is logged into AZD
if ! azd auth login --check-status &> /dev/null; then
    echo "❌ You are not logged into AZD. Please run 'azd auth login' first."
    exit 1
fi

echo "✅ Azure CLI and AZD authentication verified"

# Verify environment variables are set
echo "📋 Current AZD environment variables:"
azd env get-values

echo ""
echo "✅ Setup complete! You can now run 'azd up' to deploy your application."
echo ""
echo "📝 Next steps after deployment:"
echo "   1. Run './scripts/update-elasticsearch-secrets.sh' to configure Elasticsearch"
echo "   2. Configure the Azure OpenAI inference endpoint in Elasticsearch"
echo ""
