# Setting Up Local Secrets

This document explains how to configure your local development environment with sensitive credentials.

## Overview

The `appsettings.json` file contains **placeholder values only** and is committed to Git. Your actual credentials should be stored in `appsettings.Development.json`, which is **excluded from Git** via `.gitignore`.

## Quick Setup

### 1. Create Your Local Settings File

Copy the template and fill in your actual values:

```bash
# Navigate to the project directory
cd src/ElasticOn.RiskAgent.Demo.M365

# Create appsettings.Development.json (if it doesn't exist)
# On Windows:
Copy-Item appsettings.json appsettings.Development.json
```

### 2. Update appsettings.Development.json

Edit `appsettings.Development.json` and replace the placeholder values with your actual credentials:

```json
{
  "AIServices": {
    "AgentID": "YOUR_ACTUAL_AGENT_ID",
    "ProjectEndpoint": "https://YOUR-ACTUAL-PROJECT.services.ai.azure.com/api/projects/YOUR-PROJECT",
    "MCPTool": {
      "ServerUrl": "https://YOUR-ACTUAL-ELASTIC-CLUSTER.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp"
    },
    "ElasticApiKey": "YOUR_ACTUAL_ELASTIC_API_KEY"
  }
}
```

### 3. Verify Git Ignores Your Secrets

Check that your local settings file is ignored:

```bash
git status
```

You should **NOT** see `appsettings.Development.json` in the list of changes.

## Configuration Priority

ASP.NET Core loads configuration in this order (later sources override earlier ones):

1. **`appsettings.json`** (checked into Git - contains placeholders)
2. **`appsettings.Development.json`** (local only - contains your secrets) ?
3. Environment variables
4. User Secrets (alternative approach)

## Alternative: User Secrets (Recommended for Sensitive Data)

For even better security, use .NET User Secrets instead of `appsettings.Development.json`:

### Initialize User Secrets

```bash
cd src/ElasticOn.RiskAgent.Demo.M365
dotnet user-secrets init
```

### Set Individual Secrets

```bash
# Set Azure AI Foundry endpoint
dotnet user-secrets set "AIServices:ProjectEndpoint" "https://risk-agent-aif.services.ai.azure.com/api/projects/firstProject"

# Set Agent ID
dotnet user-secrets set "AIServices:AgentID" "asst_AZxAQqgUkkk7wNqAipgkRX5I"

# Set MCP Server URL
dotnet user-secrets set "AIServices:MCPTool:ServerUrl" "https://microsoft-build-search-demo-c81fea.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp"

# Set Elastic API Key
dotnet user-secrets set "AIServices:ElasticApiKey" "YOUR_ELASTIC_API_KEY"
```

### View All Secrets

```bash
dotnet user-secrets list
```

## What's in Each File?

### appsettings.json (Committed to Git)
- ? Non-sensitive default values
- ? Placeholder values for required settings
- ? Application structure and options
- ? No real credentials or API keys

### appsettings.Development.json (Local Only, Not in Git)
- ? Your actual development credentials
- ? Environment-specific overrides
- ? Real API keys and endpoints
- ? **NEVER commit this file**

### User Secrets (Stored Outside Project)
- ? Most secure option for local development
- ? Stored in your user profile directory
- ? Never accidentally committed
- ? Per-project isolation

## Required Settings

You must configure these values in your local environment:

### Azure AI Foundry
- **`AIServices:ProjectEndpoint`** - Your Azure AI Foundry project endpoint
- **`AIServices:AgentID`** (Optional) - Pre-existing agent ID

### MCP Tool Configuration  
- **`AIServices:MCPTool:ServerUrl`** - Your Elasticsearch MCP endpoint

### Authentication
- **`AIServices:ElasticApiKey`** - Elasticsearch API key for authentication

## Security Best Practices

### ? DO
- Use `appsettings.Development.json` for local development credentials
- Or better yet, use User Secrets for sensitive data
- Keep `appsettings.json` generic with placeholders
- Verify `.gitignore` excludes your local settings
- Use environment variables in production (Azure App Service)

### ? DON'T
- Commit real credentials to Git
- Share your `appsettings.Development.json` file
- Store production credentials in development settings
- Hardcode API keys in code

## Troubleshooting

### "Configuration value is not set" Error

If you see errors like:
```
InvalidOperationException: AIServices:ElasticApiKey is not configured
```

**Solution:** Make sure you've created `appsettings.Development.json` with your actual values, or use User Secrets.

### Git Shows appsettings.Development.json as Changed

If Git is tracking your development settings file:

```bash
# Remove from Git tracking (keeps local file)
git rm --cached src/ElasticOn.RiskAgent.Demo.M365/appsettings.Development.json

# Verify it's ignored
git status
```

### Need to Share Configuration with Team

Create a template file that can be committed:

```bash
# Create a template (committed to Git)
cp appsettings.Development.json appsettings.Development.json.template

# Edit the template to replace secrets with placeholders
# Then commit the template
git add appsettings.Development.json.template
```

## Production Deployment

For Azure deployments, use:
- **Azure App Service Application Settings** (preferred)
- **Azure Key Vault** for secrets
- **Managed Identity** for authentication

See [CONFIGURATION_GUIDE.md](CONFIGURATION_GUIDE.md) for more details.

## Related Documentation

- [CONFIGURATION_GUIDE.md](CONFIGURATION_GUIDE.md) - Complete configuration reference
- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
