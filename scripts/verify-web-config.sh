#!/bin/bash

# Script to verify the Blazor app is correctly configured for the current environment

set -e

echo "🔍 Verifying Blazor App Configuration..."

# Check if we're in the correct directory
if [ ! -f "src/ElasticOn.RiskAgent.Demo.Web/ElasticOn.RiskAgent.Demo.Web.csproj" ]; then
    echo "❌ Please run this script from the project root directory"
    exit 1
fi

cd src/ElasticOn.RiskAgent.Demo.Web

# Check which appsettings files exist
echo "📄 Configuration files found:"
if [ -f "wwwroot/appsettings.json" ]; then
    echo "  ✅ appsettings.json (production)"
    PROD_URL=$(cat wwwroot/appsettings.json | grep -o '"ApiBaseUrl":[^,]*' | cut -d'"' -f4)
    echo "     API URL: $PROD_URL"
fi

if [ -f "wwwroot/appsettings.Development.json" ]; then
    echo "  ✅ appsettings.Development.json (development)"
    DEV_URL=$(cat wwwroot/appsettings.Development.json | grep -o '"ApiBaseUrl":[^,]*' | cut -d'"' -f4)
    echo "     API URL: $DEV_URL"
fi

echo ""

# Check if Function App is running locally
if curl -s "http://localhost:7071/api" > /dev/null 2>&1; then
    echo "🟢 Local Function App appears to be running (http://localhost:7071)"
else
    echo "🔴 Local Function App is not responding (http://localhost:7071)"
    echo "   Start it with: cd src/ElasticOn.RiskAgent.Demo.Functions && func start"
fi

echo ""

# Check azd environment for production URL if available
if command -v azd >/dev/null 2>&1; then
    AZURE_FUNCTION_APP_URL=$(azd env get-values 2>/dev/null | grep AZURE_FUNCTION_APP_URL | cut -d'=' -f2 | tr -d '"' 2>/dev/null || echo "")
    if [ -n "$AZURE_FUNCTION_APP_URL" ]; then
        echo "🌐 Azure Function App URL from azd environment: $AZURE_FUNCTION_APP_URL"
        
        # Test if the Azure Function App is responding
        if curl -s -f "$AZURE_FUNCTION_APP_URL/api" > /dev/null 2>&1; then
            echo "🟢 Azure Function App is responding"
        else
            echo "🔴 Azure Function App is not responding or not deployed"
        fi
    else
        echo "⚪ No Azure environment detected or not deployed yet"
    fi
else
    echo "⚪ azd not installed - cannot check Azure environment"
fi

echo ""
echo "✅ Configuration check complete!"
echo ""
echo "🚀 To run locally:"
echo "   dotnet run"
echo ""
echo "🌐 To deploy to Azure:"
echo "   azd up"