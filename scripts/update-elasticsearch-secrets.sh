#!/bin/bash

# Update Elasticsearch configuration in Azure Function App
# This script updates the Elasticsearch URI and API Key directly in the Function App settings
# (Key Vault is not used due to Flex Consumption plan limitations)

set -e

echo "🔐 Updating Elasticsearch configuration in Azure Function App..."

# Get the Function App name and resource group from azd environment
FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME)
RESOURCE_GROUP=$(azd env get-value AZURE_RESOURCE_GROUP)

if [ -z "$FUNCTION_APP_NAME" ] || [ -z "$RESOURCE_GROUP" ]; then
    echo "❌ Error: Could not retrieve Function App name or Resource Group from azd environment"
    echo "   Make sure you have run 'azd provision' first"
    exit 1
fi

echo "🔍 Found Function App: $FUNCTION_APP_NAME in Resource Group: $RESOURCE_GROUP"
echo ""

# Prompt for Elasticsearch configuration
read -p "📝 Enter your Elasticsearch URI (e.g., https://your-cluster.es.eastus.azure.elastic.cloud:443): " ELASTICSEARCH_URI
read -p "📝 Enter your Elasticsearch API Key: " ELASTICSEARCH_API_KEY

if [ -z "$ELASTICSEARCH_URI" ] || [ -z "$ELASTICSEARCH_API_KEY" ]; then
    echo "❌ Error: Both Elasticsearch URI and API Key are required"
    exit 1
fi

echo ""
echo "🔧 Updating Function App settings..."

# Update Function App settings directly (no Key Vault)
az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings \
        "ElasticsearchUri=$ELASTICSEARCH_URI" \
        "ElasticsearchApiKey=$ELASTICSEARCH_API_KEY" \
    --output none

echo "✅ Function App settings updated"
echo ""
echo "🔄 Restarting Function App to apply changes..."
az functionapp restart --name "$FUNCTION_APP_NAME" --resource-group "$RESOURCE_GROUP" --output none
echo "✅ Function App restarted"

echo ""
echo "📝 Your Function App is now configured with:"
echo "   • Elasticsearch URI: $ELASTICSEARCH_URI"
echo "   • API Key: [HIDDEN]"
echo "   • Index Name: risk-agent-documents-v2 (configurable in Function App settings)"
echo ""
echo "🔄 The Function App may take a few minutes to pick up the new configuration."
echo "   You can monitor logs in Azure Portal or using 'azd monitor'."
echo ""
echo "⚠️  Note: Secrets are stored directly in Function App settings (not in Key Vault)"
echo "   This is due to Key Vault reference limitations in Flex Consumption plans."
echo "   TODO: Migrate to Key Vault when better support is available."
