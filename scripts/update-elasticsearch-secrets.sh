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

echo "🔍 Found Function App: $FUNCTION_APP_NAME in Resource Group: $RESOURCE_GROUP_NAME"
echo ""

# Check if local.settings.json exists
LOCAL_SETTINGS_FILE="src/ElasticOn.RiskAgent.Demo.Functions/local.settings.json"
USE_LOCAL_SETTINGS=false

if [ -f "$LOCAL_SETTINGS_FILE" ]; then
    echo "📖 Found local.settings.json with configuration values"
    echo ""
    echo "Would you like to:"
    echo "  1) Use values from local.settings.json (default)"
    echo "  2) Manually configure values (e.g., for production)"
    echo ""
    read -p "Enter your choice (1 or 2) [1]: " CHOICE
    CHOICE=${CHOICE:-1}
    echo ""
    
    if [ "$CHOICE" = "1" ]; then
        USE_LOCAL_SETTINGS=true
        echo "📖 Reading configuration from local.settings.json..."
        
        ELASTICSEARCH_URI=$(jq -r '.Values.ElasticsearchUri // empty' "$LOCAL_SETTINGS_FILE")
        ELASTICSEARCH_API_KEY=$(jq -r '.Values.ElasticsearchApiKey // empty' "$LOCAL_SETTINGS_FILE")
        ELASTICSEARCH_INDEX_NAME=$(jq -r '.Values.ElasticsearchIndexName // empty' "$LOCAL_SETTINGS_FILE")
        AZURE_OPENAI_INFERENCE_ID=$(jq -r '.Values.AzureOpenAiInferenceId // empty' "$LOCAL_SETTINGS_FILE")
        
        [ -n "$ELASTICSEARCH_URI" ] && echo "   ✓ Found ElasticsearchUri: $ELASTICSEARCH_URI"
        [ -n "$ELASTICSEARCH_API_KEY" ] && echo "   ✓ Found ElasticsearchApiKey: [HIDDEN]"
        [ -n "$ELASTICSEARCH_INDEX_NAME" ] && echo "   ✓ Found ElasticsearchIndexName: $ELASTICSEARCH_INDEX_NAME"
        [ -n "$AZURE_OPENAI_INFERENCE_ID" ] && echo "   ✓ Found AzureOpenAiInferenceId: $AZURE_OPENAI_INFERENCE_ID"
        echo ""
    else
        echo "📝 Manual configuration mode selected"
        echo ""
    fi
fi

# Check if local.settings.json exists and read values from it
LOCAL_SETTINGS_FILE="src/ElasticOn.RiskAgent.Demo.Functions/local.settings.json"

if [ -f "$LOCAL_SETTINGS_FILE" ]; then
    echo "📖 Reading configuration from local.settings.json..."
    
    # Read values from local.settings.json using jq
    ELASTICSEARCH_URI=$(jq -r '.Values.ElasticsearchUri // empty' "$LOCAL_SETTINGS_FILE")
    ELASTICSEARCH_API_KEY=$(jq -r '.Values.ElasticsearchApiKey // empty' "$LOCAL_SETTINGS_FILE")
    ELASTICSEARCH_INDEX_NAME=$(jq -r '.Values.ElasticsearchIndexName // empty' "$LOCAL_SETTINGS_FILE")
    AZURE_OPENAI_INFERENCE_ID=$(jq -r '.Values.AzureOpenAiInferenceId // empty' "$LOCAL_SETTINGS_FILE")
    
    if [ -n "$ELASTICSEARCH_URI" ]; then
        echo "   ✓ Found ElasticsearchUri: $ELASTICSEARCH_URI"
    fi
    if [ -n "$ELASTICSEARCH_API_KEY" ]; then
        echo "   ✓ Found ElasticsearchApiKey: [HIDDEN]"
    fi
    if [ -n "$ELASTICSEARCH_INDEX_NAME" ]; then
        echo "   ✓ Found ElasticsearchIndexName: $ELASTICSEARCH_INDEX_NAME"
    fi
    if [ -n "$AZURE_OPENAI_INFERENCE_ID" ]; then
        echo "   ✓ Found AzureOpenAiInferenceId: $AZURE_OPENAI_INFERENCE_ID"
    fi
    echo ""
else
    echo "⚠️  local.settings.json not found at $LOCAL_SETTINGS_FILE"
    echo ""
fi

# Prompt for values if not using local settings or if values are missing
if [ "$USE_LOCAL_SETTINGS" = false ] || [ -z "$ELASTICSEARCH_URI" ]; then
    read -p "Enter Elasticsearch URI (e.g., https://your-deployment.es.region.gcp.elastic.cloud:443): " ELASTICSEARCH_URI
fi

if [ "$USE_LOCAL_SETTINGS" = false ] || [ -z "$ELASTICSEARCH_API_KEY" ]; then
    read -sp "Enter Elasticsearch API Key: " ELASTICSEARCH_API_KEY
    echo ""
fi

if [ "$USE_LOCAL_SETTINGS" = false ] || [ -z "$ELASTICSEARCH_INDEX_NAME" ]; then
    read -p "Enter Elasticsearch Index Name (e.g., risk-agent-documents): " ELASTICSEARCH_INDEX_NAME
fi

if [ "$USE_LOCAL_SETTINGS" = false ] || [ -z "$AZURE_OPENAI_INFERENCE_ID" ]; then
    read -p "Enter Azure OpenAI Inference ID (or press Enter to skip): " AZURE_OPENAI_INFERENCE_ID
fi

if [ -z "$ELASTICSEARCH_URI" ] || [ -z "$ELASTICSEARCH_API_KEY" ]; then
    echo "❌ Error: Both Elasticsearch URI and API Key are required"
    exit 1
fi

echo ""
echo "🔧 Updating Function App settings..."

# Build the MCP Server URL from Elasticsearch URI (remove port and add MCP path)
MCP_SERVER_URL=$(echo "$ELASTICSEARCH_URI" | sed 's/:[0-9]*$//' | sed 's|$|/api/agent_builder/mcp|')

# Build the settings array
SETTINGS=(
    "ElasticsearchUri=$ELASTICSEARCH_URI"
    "ElasticsearchApiKey=$ELASTICSEARCH_API_KEY"
    "ElasticsearchIndexName=$ELASTICSEARCH_INDEX_NAME"
    "AIServicesElasticApiKey=$ELASTICSEARCH_API_KEY"
    "AIServicesMCPToolServerUrl=$MCP_SERVER_URL"
)

# Add AzureOpenAiInferenceId if it was found in local.settings.json
if [ -n "$AZURE_OPENAI_INFERENCE_ID" ]; then
    SETTINGS+=("AzureOpenAiInferenceId=$AZURE_OPENAI_INFERENCE_ID")
fi

# Update Function App settings directly (no Key Vault)
az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings "${SETTINGS[@]}" \
    --output none

echo "✅ Function App settings updated"
echo ""
echo "🔄 Restarting Function App to apply changes..."
az functionapp restart --name "$FUNCTION_APP_NAME" --resource-group "$RESOURCE_GROUP" --output none
echo "✅ Function App restarted"

echo ""
echo "📝 Your Function App is now configured with:"
echo "   • Elasticsearch URI: $ELASTICSEARCH_URI"
echo "   • Elasticsearch API Key: [HIDDEN]"
echo "   • Elasticsearch Index Name: $ELASTICSEARCH_INDEX_NAME"
echo "   • AI Services Elastic API Key: [HIDDEN]"
echo "   • AI Services MCP Tool Server URL: $MCP_SERVER_URL"
if [ -n "$AZURE_OPENAI_INFERENCE_ID" ]; then
    echo "   • Azure OpenAI Inference ID: $AZURE_OPENAI_INFERENCE_ID"
fi
echo ""
echo "🔄 The Function App may take a few minutes to pick up the new configuration."
echo "   You can monitor logs in Azure Portal or using 'azd monitor'."
echo ""
echo "⚠️  Note: Secrets are stored directly in Function App settings (not in Key Vault)"
echo "   This is due to Key Vault reference limitations in Flex Consumption plans."
echo "   TODO: Migrate to Key Vault when better support is available."
