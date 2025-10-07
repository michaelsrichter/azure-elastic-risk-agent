# Azure AI Project Configuration Fix

## Problem
The Function App was throwing the error:
```
Exception: AZURE_FOUNDRY_PROJECT_ENDPOINT is not set.
```

Even though `AIServices:ProjectEndpoint` was configured in `local.settings.json`.

## Root Cause
In Azure Functions **isolated worker model**, nested JSON objects in `local.settings.json` are **NOT** automatically flattened into the configuration system. Only settings in the `Values` section are loaded as environment variables.

The original structure had:
```json
{
  "Values": { ... },
  "AIServices": {
    "ProjectEndpoint": "..."
  }
}
```

This nested `AIServices` object was not being read by the configuration system.

## Solution

### 1. Local Development (`local.settings.json`)
Moved all configuration settings into the `Values` section using colon-separated keys:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AZURE_FOUNDRY_PROJECT_ENDPOINT": "https://...",
    "AIServices:ProjectEndpoint": "https://...",
    "AIServices:ModelId": "gpt-4o-mini",
    "AIServices:Agent:Name": "RiskAgent-Demo",
    "AIServices:MCPTool:AllowedTools:0": "...",
    "AIServices:MCPTool:AllowedTools:1": "..."
  }
}
```

**Key Points:**
- Use colon (`:`) to represent nested configuration paths
- Use array indices (`:0`, `:1`) for array elements
- Both `AZURE_FOUNDRY_PROJECT_ENDPOINT` (environment variable) and `AIServices:ProjectEndpoint` (configuration key) are set for compatibility

### 2. Azure Deployment (`main.bicep`)
Added the following app settings to the Function App configuration:

```bicep
{
  name: 'AZURE_FOUNDRY_PROJECT_ENDPOINT'
  value: 'https://${aiFoundryService.properties.endpoint}/api/projects/${aiProject.name}'
}
{
  name: 'AIServices:ProjectEndpoint'
  value: 'https://${aiFoundryService.properties.endpoint}/api/projects/${aiProject.name}'
}
{
  name: 'AIServices:ModelId'
  value: gpt4oMiniDeployment.name
}
```

### 3. Infrastructure Updates
Added the following Azure resources to `main.bicep`:

1. **Key Vault** (`azkv{token}`) - For storing secrets
2. **AI Foundry Storage Account** (`azstaif{token}`) - For AI artifacts
3. **Azure AI Hub** (`aihub{token}`) - Workspace for AI projects
4. **Azure AI Project** (`aiproj{token}`) - The actual AI project
5. **AI Services Connection** - Links project to AI Services

## New Outputs
The Bicep deployment now outputs:
- `AZURE_AI_HUB_NAME` and `AZURE_AI_HUB_ID`
- `AZURE_AI_PROJECT_NAME` and `AZURE_AI_PROJECT_ID`
- `AZURE_AI_PROJECT_ENDPOINT` - The full project endpoint URL
- `AZURE_KEY_VAULT_NAME`

## Testing
To test locally:
1. Ensure `local.settings.json` has the flattened structure
2. Run your Function App
3. The `AzureAIAgentService` constructor should now successfully read the endpoint

To deploy:
```bash
azd up
```

The Function App will automatically receive the project endpoint from the Bicep outputs.

## Configuration Reading Order
The `AzureAIAgentService` checks in this order:
1. **Environment variable**: `AZURE_FOUNDRY_PROJECT_ENDPOINT`
2. **Configuration key**: `AIServices:ProjectEndpoint`
3. **Throws exception** if neither is found

With the fix, both are now set, ensuring compatibility in all environments.
