#!/bin/bash
set -e

echo "🔑 Configuring Internal Function Key..."

# Get the function app name from azd environment
FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME)
RESOURCE_GROUP=$(azd env get-value AZURE_RESOURCE_GROUP)

if [ -z "$FUNCTION_APP_NAME" ] || [ -z "$RESOURCE_GROUP" ]; then
    echo "❌ Error: Could not find AZURE_FUNCTION_APP_NAME or AZURE_RESOURCE_GROUP in azd environment"
    echo "Please run 'azd env refresh' first"
    exit 1
fi

echo "📍 Function App: $FUNCTION_APP_NAME"
echo "📍 Resource Group: $RESOURCE_GROUP"
echo ""

# Get the master key (host key) which can be used for any function
echo "🔍 Retrieving function host key..."
FUNCTION_KEY=$(az functionapp keys list \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "functionKeys.default" \
    -o tsv)

if [ -z "$FUNCTION_KEY" ]; then
    echo "⚠️  Default function key not found. Creating one..."
    
    # Create a new function key
    FUNCTION_KEY=$(az functionapp keys set \
        --name "$FUNCTION_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --key-type functionKeys \
        --key-name "internal" \
        --query "value" \
        -o tsv)
fi

if [ -z "$FUNCTION_KEY" ]; then
    echo "❌ Error: Could not retrieve or create function key"
    exit 1
fi

echo "✅ Retrieved function key: ${FUNCTION_KEY:0:10}***"
echo ""

# Update the INTERNAL_FUNCTION_KEY app setting
echo "🔧 Updating INTERNAL_FUNCTION_KEY app setting..."
az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings "INTERNAL_FUNCTION_KEY=$FUNCTION_KEY" \
    --output none

echo "✅ INTERNAL_FUNCTION_KEY configured successfully!"
echo ""

# Also set it in the azd environment for reference
azd env set INTERNAL_FUNCTION_KEY "$FUNCTION_KEY" > /dev/null 2>&1

echo "🎉 Configuration complete!"
echo ""
echo "📝 The function key has been:"
echo "   • Set as INTERNAL_FUNCTION_KEY in Function App settings"
echo "   • Saved in azd environment variables"
echo ""
echo "🔄 The Function App will automatically use this key for internal calls."
echo "   You may need to restart the Function App if it's currently running:"
echo ""
echo "   az functionapp restart --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP"
