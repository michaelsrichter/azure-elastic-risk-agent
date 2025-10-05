# Azure AI Agent Service Configuration Guide

This document explains the configuration options for the Azure AI Agent Service.

## Configuration Structure

All configuration is stored in `appsettings.json` under the `AIServices` section.

### Complete Configuration Example

```json
{
  "AIServices": {
    "AgentID": "asst_AZxAQqgUkkk7wNqAipgkRX5I",
    "ProjectEndpoint": "https://risk-agent-aif.services.ai.azure.com/api/projects/firstProject",
    "ModelId": "gpt-4.1-mini",
    "Agent": {
      "Name": "RiskAgent",
      "Instructions": "You answer questions about Risk based on what you find in the context."
    },
    "MCPTool": {
      "ServerLabel": "elastic_search_mcp",
      "ServerUrl": "https://microsoft-build-search-demo-c81fea.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp",
      "AllowedTools": [
        "azure_elastic_risk_agent.search_docs"
      ]
    },
    "ElasticApiKey": "YOUR_ELASTIC_API_KEY_HERE"
  }
}
```

## Configuration Options

### Required Settings

#### `ProjectEndpoint`
- **Type**: `string`
- **Environment Variable**: `AZURE_FOUNDRY_PROJECT_ENDPOINT`
- **Description**: The Azure AI Foundry project endpoint URL
- **Example**: `"https://risk-agent-aif.services.ai.azure.com/api/projects/firstProject"`

#### `Agent:Name`
- **Type**: `string`
- **Description**: The name of the agent to create
- **Example**: `"RiskAgent"`

#### `Agent:Instructions`
- **Type**: `string`
- **Description**: System instructions for the agent
- **Example**: `"You answer questions about Risk based on what you find in the context."`

#### `MCPTool:ServerLabel`
- **Type**: `string`
- **Description**: Label for the MCP server
- **Example**: `"elastic_search_mcp"`

#### `MCPTool:ServerUrl`
- **Type**: `string`
- **Description**: URL of the MCP server endpoint
- **Example**: `"https://microsoft-build-search-demo-c81fea.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp"`

#### `MCPTool:AllowedTools`
- **Type**: `string[]`
- **Description**: Array of allowed tool names for the MCP server
- **Example**: `["azure_elastic_risk_agent.search_docs"]`

#### `ElasticApiKey`
- **Type**: `string`
- **Description**: API key for authenticating with Elasticsearch
- **Security Note**: ?? Store securely. Consider using environment variables or Azure Key Vault for production.
- **Example**: `"YOUR_ELASTIC_API_KEY_HERE"`

### Optional Settings

#### `ModelId`
- **Type**: `string`
- **Environment Variable**: `AZURE_FOUNDRY_PROJECT_MODEL_ID`
- **Default**: `"gpt-4.1-mini"`
- **Description**: The AI model to use for the agent
- **Example**: `"gpt-4o"`, `"gpt-4.1-mini"`

#### `AgentID`
- **Type**: `string`
- **Description**: Pre-existing agent ID to use (if you want to reuse an existing agent instead of creating a new one)
- **Example**: `"asst_AZxAQqgUkkk7wNqAipgkRX5I"`
- **Note**: This is currently in the config but not actively used by `GetOrCreateAgentAsync`. The agent ID is managed in conversation state.

## Environment Variables

You can override configuration values using environment variables:

### `AZURE_FOUNDRY_PROJECT_ENDPOINT`
Overrides `AIServices:ProjectEndpoint`

```bash
# Windows (PowerShell)
$env:AZURE_FOUNDRY_PROJECT_ENDPOINT="https://your-project.services.ai.azure.com/api/projects/yourProject"

# Linux/Mac
export AZURE_FOUNDRY_PROJECT_ENDPOINT="https://your-project.services.ai.azure.com/api/projects/yourProject"
```

### `AZURE_FOUNDRY_PROJECT_MODEL_ID`
Overrides `AIServices:ModelId`

```bash
# Windows (PowerShell)
$env:AZURE_FOUNDRY_PROJECT_MODEL_ID="gpt-4o"

# Linux/Mac
export AZURE_FOUNDRY_PROJECT_MODEL_ID="gpt-4o"
```

## Development Configuration

For local development, create `appsettings.Development.json` to override settings:

```json
{
  "AIServices": {
    "Agent": {
      "Name": "RiskAgent-Dev",
      "Instructions": "You answer questions about Risk based on what you find in the context. [Development Mode]"
    },
    "MCPTool": {
      "ServerLabel": "elastic_search_mcp_dev",
      "ServerUrl": "https://your-dev-elastic-cluster.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp",
      "AllowedTools": [
        "azure_elastic_risk_agent.search_docs"
      ]
    },
    "ElasticApiKey": "YOUR_DEV_ELASTIC_API_KEY_HERE"
  }
}
```

## Security Best Practices

### Storing Secrets

?? **Never commit secrets to source control!**

For production environments, consider:

1. **Azure Key Vault**: Store `ElasticApiKey` in Key Vault
2. **Environment Variables**: Use Azure App Service application settings
3. **User Secrets**: For local development only

### Using User Secrets (Local Development)

```bash
# Initialize user secrets
cd src/ElasticOn.RiskAgent.Demo.M365
dotnet user-secrets init

# Set the Elastic API key
dotnet user-secrets set "AIServices:ElasticApiKey" "YOUR_API_KEY_HERE"
```

### Using Azure Key Vault (Production)

Update your `Program.cs` to include Key Vault:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

Store secrets in Key Vault:
- Key: `AIServices--ElasticApiKey`
- Value: Your Elastic API key

## Validation

The service validates all required configuration on startup. If any required setting is missing, it will throw an `InvalidOperationException` with a descriptive message.

## Configuration Hierarchy

ASP.NET Core loads configuration in this order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
3. User secrets (Development environment only)
4. Environment variables
5. Command-line arguments

## Testing Configuration

To verify your configuration is loaded correctly, check the application logs on startup:

```
info: ElasticOn.RiskAgent.Demo.M365.Services.AzureAIAgentService[0]
      Initializing Azure AI Agent Service with endpoint: https://risk-agent-aif.services.ai.azure.com/api/projects/firstProject
```

## Troubleshooting

### Common Configuration Errors

#### "AIServices:Agent:Name is not configured"
**Solution**: Add the `Agent:Name` setting to your `appsettings.json`

#### "AZURE_FOUNDRY_PROJECT_ENDPOINT is not set"
**Solution**: Either set the environment variable or add `AIServices:ProjectEndpoint` to your configuration

#### "AIServices:MCPTool:AllowedTools is not configured"
**Solution**: Ensure `AllowedTools` is defined as a JSON array in your configuration

### Checking Current Configuration

Add this to your service for debugging:

```csharp
_logger.LogDebug("Agent Name: {AgentName}", _agentName);
_logger.LogDebug("MCP Server: {ServerUrl}", _mcpServerUrl);
_logger.LogDebug("Allowed Tools: {Tools}", string.Join(", ", _mcpAllowedTools));
```

## Related Documentation

- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-services/)
- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/aspnet/core/security/key-vault-configuration)
