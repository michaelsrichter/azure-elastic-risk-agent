#!/bin/bash

# Script to set up Azure environment variables for deployment
# This script helps configure the required environment variables for the risk-agent project

set -e

echo "=========================================="
echo "Azure Risk Agent - Environment Setup"
echo "=========================================="
echo ""

# Get the Azure environment name
read -p "Enter your Azure environment name (e.g., dev, prod): " AZURE_ENV
if [ -z "$AZURE_ENV" ]; then
    echo "Error: Environment name is required"
    exit 1
fi

# Check if azd is installed
if ! command -v azd &> /dev/null; then
    echo "Error: Azure Developer CLI (azd) is not installed"
    echo "Install it from: https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd"
    exit 1
fi

# Set the environment
echo "Setting Azure environment to: $AZURE_ENV"
azd env select $AZURE_ENV || azd env new $AZURE_ENV

echo ""
echo "=========================================="
echo "Required Configuration"
echo "=========================================="
echo ""

# MCP Server URL (Required)
read -p "Enter MCP Server URL (e.g., https://your-cluster.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp): " MCP_URL
if [ -z "$MCP_URL" ]; then
    echo "Error: MCP Server URL is required"
    exit 1
fi
azd env set MCP_SERVER_URL "$MCP_URL"
echo "✓ MCP_SERVER_URL set"

# Elastic API Key (Required)
read -sp "Enter Elastic API Key: " ELASTIC_KEY
echo ""
if [ -z "$ELASTIC_KEY" ]; then
    echo "Error: Elastic API Key is required"
    exit 1
fi
azd env set ELASTIC_API_KEY "$ELASTIC_KEY"
echo "✓ ELASTIC_API_KEY set"

echo ""
echo "=========================================="
echo "Optional Configuration"
echo "=========================================="
echo ""

# Content Safety Endpoint (Optional)
read -p "Enter Content Safety Endpoint (press Enter to skip): " CS_ENDPOINT
if [ -n "$CS_ENDPOINT" ]; then
    azd env set CONTENT_SAFETY_ENDPOINT "$CS_ENDPOINT"
    echo "✓ CONTENT_SAFETY_ENDPOINT set"
    
    # Content Safety Key (Optional)
    read -sp "Enter Content Safety Subscription Key (press Enter to skip): " CS_KEY
    echo ""
    if [ -n "$CS_KEY" ]; then
        azd env set CONTENT_SAFETY_SUBSCRIPTION_KEY "$CS_KEY"
        echo "✓ CONTENT_SAFETY_SUBSCRIPTION_KEY set"
    fi
fi

# Agent Name (Optional)
read -p "Enter Agent Name (default: RiskAgent-Demo): " AGENT_NAME
if [ -n "$AGENT_NAME" ]; then
    azd env set AI_AGENT_NAME "$AGENT_NAME"
    echo "✓ AI_AGENT_NAME set"
fi

# MCP Server Label (Optional)
read -p "Enter MCP Server Label (default: elastic_search_mcp): " MCP_LABEL
if [ -n "$MCP_LABEL" ]; then
    azd env set MCP_SERVER_LABEL "$MCP_LABEL"
    echo "✓ MCP_SERVER_LABEL set"
fi

echo ""
echo "=========================================="
echo "Configuration Summary"
echo "=========================================="
echo ""
echo "Environment: $AZURE_ENV"
echo "MCP Server URL: $MCP_URL"
echo "Elastic API Key: [HIDDEN]"
if [ -n "$CS_ENDPOINT" ]; then
    echo "Content Safety Endpoint: $CS_ENDPOINT"
fi
if [ -n "$AGENT_NAME" ]; then
    echo "Agent Name: $AGENT_NAME"
fi
if [ -n "$MCP_LABEL" ]; then
    echo "MCP Server Label: $MCP_LABEL"
fi

echo ""
echo "=========================================="
echo "Next Steps"
echo "=========================================="
echo ""
echo "Your environment has been configured. To deploy:"
echo ""
echo "  azd up                  # Deploy everything (infrastructure + code)"
echo "  azd provision           # Deploy infrastructure only"
echo "  azd deploy              # Deploy code only"
echo ""
echo "To view your environment variables:"
echo ""
echo "  azd env get-values"
echo ""
echo "To modify a variable later:"
echo ""
echo "  azd env set VARIABLE_NAME \"value\""
echo ""
echo "Configuration complete! ✓"
