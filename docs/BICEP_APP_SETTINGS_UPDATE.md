# Bicep App Settings Configuration

## Overview
All AIServices configuration from `local.settings.json` has been added to the Bicep deployment as Function App settings. This ensures consistent configuration between local development and Azure deployment.

## Changes Made

### 1. Added Parameters to `main.bicep`
The following parameters were added to make the configuration flexible and environment-specific:

| Parameter | Default Value | Description |
|-----------|---------------|-------------|
| `agentId` | `''` (empty) | Optional pre-configured Agent ID |
| `agentName` | `'RiskAgent-Demo'` | Name of the AI agent |
| `agentInstructions` | Full instructions | The system prompt/instructions for the agent |
| `mcpServerLabel` | `'elastic_search_mcp'` | Label for the MCP server |
| `mcpServerUrl` | (required) | URL to the Elastic MCP server endpoint |
| `mcpAllowedTools` | Comma-separated list | Tools the agent can use |
| `elasticApiKey` | (required) | API key for authenticating with Elastic |
| `contentSafetyEndpoint` | `''` (empty) | Azure Content Safety endpoint |
| `contentSafetySubscriptionKey` | `''` (empty) | Content Safety subscription key (secure) |
| `contentSafetyJailbreakDetectionMode` | `'Audit'` | Detection mode for jailbreak attempts |

### 2. Added App Settings to Function App
All the above parameters are mapped to app settings in the Function App:

```bicep
{
  name: 'AZURE_FOUNDRY_PROJECT_ENDPOINT'
  value: 'https://${aiFoundryService.properties.endpoint}/api/projects/${aiProject.name}'
}
{
  name: 'AIServices:AgentID'
  value: agentId
}
{
  name: 'AIServices:ProjectEndpoint'
  value: 'https://${aiFoundryService.properties.endpoint}/api/projects/${aiProject.name}'
}
{
  name: 'AIServices:ModelId'
  value: gpt4oMiniDeployment.name
}
{
  name: 'AIServices:Agent:Name'
  value: agentName
}
{
  name: 'AIServices:Agent:Instructions'
  value: agentInstructions
}
{
  name: 'AIServices:MCPTool:ServerLabel'
  value: mcpServerLabel
}
{
  name: 'AIServices:MCPTool:ServerUrl'
  value: mcpServerUrl
}
{
  name: 'AIServices:MCPTool:AllowedTools:0'
  value: split(mcpAllowedTools, ',')[0]
}
{
  name: 'AIServices:MCPTool:AllowedTools:1'
  value: length(split(mcpAllowedTools, ',')) > 1 ? split(mcpAllowedTools, ',')[1] : ''
}
{
  name: 'AIServices:ElasticApiKey'
  value: elasticApiKey
}
{
  name: 'AIServices:ContentSafety:Endpoint'
  value: contentSafetyEndpoint
}
{
  name: 'AIServices:ContentSafety:SubscriptionKey'
  value: contentSafetySubscriptionKey
}
{
  name: 'AIServices:ContentSafety:JailbreakDetectionMode'
  value: contentSafetyJailbreakDetectionMode
}
```

### 3. Updated `main.parameters.json`
Environment variable references were added to allow configuration via `.env` file or environment variables:

```json
{
  "agentId": { "value": "${AI_AGENT_ID=}" },
  "agentName": { "value": "${AI_AGENT_NAME=RiskAgent-Demo}" },
  "mcpServerUrl": { "value": "${MCP_SERVER_URL}" },
  "elasticApiKey": { "value": "${ELASTIC_API_KEY}" },
  "contentSafetyEndpoint": { "value": "${CONTENT_SAFETY_ENDPOINT=}" },
  "contentSafetySubscriptionKey": { "value": "${CONTENT_SAFETY_SUBSCRIPTION_KEY=}" }
}
```

## Required Environment Variables

To deploy, you need to set the following environment variables in your `.azure/<env-name>/.env` file or as environment variables:

### **Required Variables**
```bash
# Elastic Configuration (Required)
MCP_SERVER_URL="https://your-cluster.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp"
ELASTIC_API_KEY="your-elasticsearch-api-key-here"
```

### **Optional Variables with Defaults**
```bash
# Agent Configuration (Optional - has defaults)
AI_AGENT_ID=""
AI_AGENT_NAME="RiskAgent-Demo"
MCP_SERVER_LABEL="elastic_search_mcp"
MCP_ALLOWED_TOOLS="azure_elastic_risk_agent_search_docs,azure_elastic_risk_agent_docs_list"

# Content Safety Configuration (Optional)
CONTENT_SAFETY_ENDPOINT="https://your-content-safety.cognitiveservices.azure.com/"
CONTENT_SAFETY_SUBSCRIPTION_KEY="your-subscription-key"
CONTENT_SAFETY_JAILBREAK_DETECTION_MODE="Audit"

# Agent Instructions (Optional - has default)
AI_AGENT_INSTRUCTIONS="Role: You are a highly professional..."
```

## Setting Environment Variables

### Option 1: Using `.env` file (Recommended)
Create or update `.azure/<env-name>/.env`:

```bash
# Example: .azure/dev/.env
MCP_SERVER_URL=https://microsoft-build-search-demo-c81fea.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp
ELASTIC_API_KEY=SGFNUDA1Z0J1VURrVDJ0Zk15M0s6eHA2MC16aHNqRGNmZUYwbGZTWUE2QQ==
CONTENT_SAFETY_ENDPOINT=https://elastic-ai-roi-foundry.cognitiveservices.azure.com/
CONTENT_SAFETY_SUBSCRIPTION_KEY=bwGpaClZpsmzbjuaCIFbdJIodbvPX8Xyfp18Hzz5OQIoOKBGhBH2JQQJ99BFACYeBjFXJ3w3AAAAACOGopMo
```

### Option 2: Using `azd env set`
```bash
azd env set MCP_SERVER_URL "https://your-cluster.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp"
azd env set ELASTIC_API_KEY "your-api-key"
azd env set CONTENT_SAFETY_ENDPOINT "https://your-content-safety.cognitiveservices.azure.com/"
azd env set CONTENT_SAFETY_SUBSCRIPTION_KEY "your-subscription-key"
```

### Option 3: Export in shell
```bash
export MCP_SERVER_URL="https://your-cluster.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp"
export ELASTIC_API_KEY="your-api-key"
```

## Deployment

Once environment variables are set, deploy with:

```bash
# Deploy everything
azd up

# Or just infrastructure
azd provision

# Or just the Function App code
azd deploy api
```

## Verification

After deployment, verify the settings in the Azure Portal:
1. Go to your Function App
2. Click on **Configuration** → **Application settings**
3. Look for all the `AIServices:*` settings
4. Verify they have the correct values

Or use Azure CLI:
```bash
az functionapp config appsettings list \
  --name <your-function-app-name> \
  --resource-group <your-resource-group> \
  --query "[?starts_with(name, 'AIServices')].{Name:name, Value:value}" \
  --output table
```

## Local Development vs. Azure

| Configuration | Local Development | Azure Deployment |
|---------------|-------------------|------------------|
| Source | `local.settings.json` | Bicep `appSettings` |
| Format | JSON with nested objects flattened to `Values` | Bicep parameters → App Settings |
| AI Project Endpoint | Hardcoded in `local.settings.json` | Dynamically generated from Bicep resources |
| Secrets | Plain text in `local.settings.json` | Can use Key Vault references or `@secure()` parameters |

## Security Notes

⚠️ **Important Security Considerations:**

1. **Never commit secrets to git** - Add `.azure/*/.env` to `.gitignore`
2. **Use Key Vault for production** - Consider using Key Vault references for sensitive values:
   ```bicep
   {
     name: 'AIServices:ElasticApiKey'
     value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/elastic-api-key/)'
   }
   ```
3. **Rotate keys regularly** - Especially for Elastic API keys and Content Safety keys
4. **Use managed identities** where possible - Already configured for Azure OpenAI

## Next Steps

1. Set the required environment variables in your `.azure/<env-name>/.env` file
2. Run `azd provision` to update the infrastructure
3. Test the Function App to ensure all configuration is loaded correctly
4. Consider moving secrets to Key Vault for production deployments
