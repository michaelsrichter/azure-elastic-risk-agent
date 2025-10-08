# Microsoft 365 Agents Toolkit - Risk Agent Deployment

This folder contains the Microsoft 365 Agents Toolkit configuration for deploying the Risk Agent bot to Microsoft Teams.

## Overview

The Risk Agent bot is a Microsoft Teams bot that provides intelligent risk assessment conversations powered by Azure AI Foundry. The actual bot implementation is in the `src/ElasticOn.RiskAgent.Demo.M365` project. This folder contains the deployment configuration and manifest for Microsoft Teams.

## Project Structure

- `appPackage/` - Teams app manifest and assets
- `infra/` - Infrastructure as code for Teams deployment
- `env/` - Environment configuration files
- `m365agents.yml` - Agents Toolkit configuration

## Quick Start

### Local Development

1. **Open in Visual Studio**: Open the `M365Agent.atkproj` file
2. **Press F5**: Start debugging to launch the bot in Microsoft 365 Agents Playground
3. **Test the bot**: Type questions about risk to interact with the bot

![Debug in Visual Studio](https://raw.githubusercontent.com/OfficeDev/TeamsFx/dev/docs/images/visualstudio/debug/debug-button.png)

### Prerequisites

- Visual Studio 2022 with Microsoft 365 Agents Toolkit extension
- Microsoft 365 developer account
- Azure AI Foundry project with deployed agent
- Elasticsearch cluster for document search

### Configuration

The bot requires the following services to be configured in `src/ElasticOn.RiskAgent.Demo.M365/appsettings.json`:

- **Azure AI Foundry** - Project endpoint and model ID
- **Elasticsearch** - MCP server URL and API key
- **Azure Content Safety** - Endpoint and subscription key (optional)
- **Microsoft Teams** - App ID and password

See [../docs/M365-CONFIGURATION_GUIDE.md](../docs/M365-CONFIGURATION_GUIDE.md) for detailed configuration instructions.

## Deployment

### Deploy to Teams

Using Microsoft 365 Agents Toolkit:
1. Sign in to your Microsoft 365 account
2. Build and deploy the Teams app package
3. The bot will be available in Teams for your organization

### Run on Other Platforms

The bot can also run in:
- Microsoft Outlook
- Microsoft 365 app

See https://aka.ms/vs-ttk-debug-multi-profiles for more details.

## Documentation

- [M365 Implementation Summary](../docs/M365-IMPLEMENTATION_SUMMARY.md) - Bot architecture and features
- [M365 Configuration Guide](../docs/M365-CONFIGURATION_GUIDE.md) - Configuration details
- [M365 Agent Integration](../docs/M365-AGENT_INTEGRATION.md) - Agent integration details
- [M365 Setup Secrets](../docs/M365-SETUP_SECRETS.md) - Secret management

## Learn More

- [Microsoft 365 Agents Toolkit documentation](https://aka.ms/teams-toolkit-vs-docs)
- [Teams app manifests](https://docs.microsoft.com/microsoftteams/platform/resources/schema/manifest-schema)
- [Azure AI Foundry](https://learn.microsoft.com/azure/ai-studio/)

## Support

- **Teams Toolkit Issues**: https://github.com/OfficeDev/TeamsFx/issues
- **Risk Agent Issues**: Use this repository's issue tracker
