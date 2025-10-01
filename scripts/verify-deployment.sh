#!/bin/bash
set -e

echo "===================================="
echo "Verifying Function App Deployment"
echo "===================================="

# Get environment variables
FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME)
RESOURCE_GROUP=$(azd env get-value AZURE_RESOURCE_GROUP)

echo ""
echo "Function App: $FUNCTION_APP_NAME"
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# Check app status
echo "📊 Checking Function App status..."
STATE=$(az functionapp show --resource-group "$RESOURCE_GROUP" --name "$FUNCTION_APP_NAME" --query "state" -o tsv)
echo "   State: $STATE"

# Check runtime version
echo ""
echo "🔧 Runtime configuration..."
az functionapp config show --resource-group "$RESOURCE_GROUP" --name "$FUNCTION_APP_NAME" --query "{NetFramework:netFrameworkVersion,AlwaysOn:alwaysOn,Http20Enabled:http20Enabled}" -o table

# Check if functions are loaded
echo ""
echo "📦 Checking deployed functions (this may take a moment)..."
FUNCTIONS=$(az functionapp function list --resource-group "$RESOURCE_GROUP" --name "$FUNCTION_APP_NAME" --query "[].name" -o tsv 2>/dev/null || echo "")

if [ -z "$FUNCTIONS" ]; then
    echo "   ⏳ Functions are still initializing. This usually takes 1-2 minutes."
    echo ""
    echo "   You can check manually with:"
    echo "   az functionapp function list --resource-group $RESOURCE_GROUP --name $FUNCTION_APP_NAME"
else
    echo "   ✅ Functions found:"
    echo "$FUNCTIONS" | sed 's/^/      - /'
fi

# Get function app URL
echo ""
echo "🌐 Function App URL:"
HOSTNAME=$(az functionapp show --resource-group "$RESOURCE_GROUP" --name "$FUNCTION_APP_NAME" --query "defaultHostName" -o tsv)
echo "   https://$HOSTNAME"

# Check function keys
echo ""
echo "🔑 Getting function keys..."
echo "   Run this command to get the master key:"
echo "   az functionapp keys list --resource-group $RESOURCE_GROUP --name $FUNCTION_APP_NAME"

echo ""
echo "✅ Verification complete!"
