# Documentation Index

Quick reference to all documentation in this repository.

## Main Documentation

### [README.md](../README.md)
**Start here!** Overview of the entire solution.
- Complete solution overview (Functions, M365 Bot, Web App)
- Architecture diagram
- Key features and technologies
- Prerequisites and local development
- Quick deployment with `azd up`
- Comprehensive documentation links

### [AZURE_DEPLOYMENT.md](../AZURE_DEPLOYMENT.md)
Complete deployment guide for Azure.
- Step-by-step deployment instructions
- Architecture overview
- Configuration management (Elasticsearch, Azure OpenAI)
- Viewing logs in Application Insights
- Testing deployed functions
- Troubleshooting common issues
- Important notes on authentication and security

## Azure Functions Documentation

### [ChatFunction-README.md](./ChatFunction-README.md)
Technical documentation for the Chat function.
- Request/response format
- Azure AI Foundry Agent integration
- Elasticsearch MCP tool integration
- Content Safety integration
- Conversation state management

### [ChatFunction-IMPLEMENTATION-SUMMARY.md](./ChatFunction-IMPLEMENTATION-SUMMARY.md)
Implementation summary for the Chat function.
- High-level architecture
- Key components and services
- Configuration options

### [IndexDocumentFunction-README.md](./IndexDocumentFunction-README.md)
Technical documentation for the IndexDocument function.
- Request/response format
- Elasticsearch integration
- Custom configuration options

### [ProcessPdfFunction-IndexDocument-README.md](./ProcessPdfFunction-IndexDocument-README.md)
Technical documentation for the ProcessPdf function.
- PDF processing workflow
- Chunking strategy
- Integration with IndexDocument function

### [Functions-ELASTICSEARCH_CONFIG_EXAMPLES.md](./Functions-ELASTICSEARCH_CONFIG_EXAMPLES.md)
Elasticsearch configuration examples for Azure Functions.
- Connection configuration
- Index configuration
- Azure OpenAI inference setup

## Microsoft Teams Bot Documentation

### [M365-IMPLEMENTATION_SUMMARY.md](./M365-IMPLEMENTATION_SUMMARY.md)
Implementation overview for the Microsoft Teams bot.
- Architecture and key components
- Bot functionality
- AI services integration

### [M365-CONFIGURATION_GUIDE.md](./M365-CONFIGURATION_GUIDE.md)
Configuration guide for the Teams bot.
- Azure AI Foundry setup
- Elasticsearch MCP configuration
- Content Safety setup
- Teams bot credentials

### [M365-AGENT_INTEGRATION.md](./M365-AGENT_INTEGRATION.md)
Agent integration details for the Teams bot.
- Agent creation and management
- MCP tool integration
- Conversation flow

### [M365-SETUP_SECRETS.md](./M365-SETUP_SECRETS.md)
Secret management for the Teams bot.
- Required secrets
- Configuration files
- Environment variables

## Blazor Web App Documentation

### [Web-README.md](./Web-README.md)
Overview of the Blazor WebAssembly chat application.
- Features and architecture
- Chat interface
- Configuration
- Backend API integration

### [Web-CLARITY_SETUP.md](./Web-CLARITY_SETUP.md)
Microsoft Clarity analytics setup.
- Clarity integration
- Configuration steps
- Privacy considerations

### [Web-LAYOUT_UPDATES.md](./Web-LAYOUT_UPDATES.md)
Layout and UI updates documentation.
- Design changes
- Component structure
- Styling approach

## Content Safety Documentation

### [ContentSafety.md](./ContentSafety.md)
Complete technical reference for Content Safety.
- Overview and features
- Architecture
- Configuration
- Usage examples
- API details
- Error handling
- Extending the service

### [ContentSafetyExamples.md](./ContentSafetyExamples.md)
Practical examples and scenarios.
- Configuration setup
- Basic usage
- Integration examples
- Advanced scenarios
- Best practices

### [ContentSafety_QUICKSTART.md](./ContentSafety_QUICKSTART.md)
Quick start guide (5 minutes).
- Prerequisites
- Azure resource creation
- Application configuration
- Testing and verification

### [ContentSafety_IMPLEMENTATION_SUMMARY.md](./ContentSafety_IMPLEMENTATION_SUMMARY.md)
High-level implementation overview.
- What was implemented
- Key features
- Integration flow
- Performance considerations

### [ContentSafety_DetectionModes_Update.md](./ContentSafety_DetectionModes_Update.md)
Detection modes documentation.
- Disabled, Audit, Enforce modes
- Use cases for each mode
- Configuration

### [ContentSafety_JSON_Extraction.md](./ContentSafety_JSON_Extraction.md)
JSON extraction optimization.
- Performance improvements
- Cost reduction
- Implementation details

### [ContentSafety_Optimization_Changes.md](./ContentSafety_Optimization_Changes.md)
Performance optimization changes.
- Optimization strategies
- Before/after metrics
- Best practices

### [ContentSafety_API_Response_Fix.md](./ContentSafety_API_Response_Fix.md)
API response property fix documentation.
- Bug fix details
- API changes
- Migration guide

### [CONTENT_SAFETY_MODE_OVERRIDE.md](./CONTENT_SAFETY_MODE_OVERRIDE.md)
Content Safety mode override feature.
- Per-request overrides
- Use cases
- Configuration

## Reference Documentation

### [CUSTOM_ELASTICSEARCH_INDEX.md](./CUSTOM_ELASTICSEARCH_INDEX.md)
Custom Elasticsearch index configuration.
- Index mapping
- Semantic text fields
- Azure OpenAI inference setup
- Custom analyzers

### [CORS_CONFIGURATION.md](./CORS_CONFIGURATION.md)
CORS configuration for Azure Functions.
- CORS setup
- Allowed origins
- Security considerations

### [MARKDIG_INTEGRATION.md](./MARKDIG_INTEGRATION.md)
Markdig markdown parsing integration.
- Usage and configuration
- Rendering options

### [UPDATE_ELASTICSEARCH_SECRETS.md](./UPDATE_ELASTICSEARCH_SECRETS.md)
Script documentation for updating Elasticsearch secrets.
- Script usage
- Configuration values
- Automation

### [AI_PROJECT_CONFIGURATION_FIX.md](./AI_PROJECT_CONFIGURATION_FIX.md)
AI project configuration fix documentation.
- Configuration issues
- Resolution steps

### [BICEP_APP_SETTINGS_UPDATE.md](./BICEP_APP_SETTINGS_UPDATE.md)
Bicep infrastructure updates for app settings.
- Infrastructure changes
- Configuration deployment

### [DOCUMENTATION_UPDATE_SUMMARY.md](./DOCUMENTATION_UPDATE_SUMMARY.md)
Documentation update summary.
- Changes made
- Accuracy checklist
- Version information

## Change History Documentation

### [CONTENT_SAFETY_CHANGES.md](./CONTENT_SAFETY_CHANGES.md)
Original Content Safety v1.0 implementation summary.
- Files created and modified
- Implementation details
- Testing coverage

### [CONTENT_SAFETY_CHANGES_V2.md](./CONTENT_SAFETY_CHANGES_V2.md)
Complete Content Safety v2.0 change summary.
- Version history
- Latest optimizations
- Migration guide
- Performance improvements

## Quick Links

### Deployment
```bash
# First time setup
azd up

# Update Elasticsearch configuration
./scripts/update-elasticsearch-secrets.sh

# Configure function authentication
./scripts/configure-function-key.sh
```

### Monitoring
- **Health Check**: `https://YOUR-FUNCTION-APP.azurewebsites.net/api/health`
- **Application Insights**: Azure Portal → Your Function App → Application Insights → Logs
- **Live Metrics**: Application Insights → Live Metrics

### Configuration Files
- `azure.yaml` - AZD deployment configuration
- `infra/main.bicep` - Azure infrastructure as code
- `infra/main.parameters.json` - Deployment parameters
- `host.json` - Azure Functions host configuration
- `local.settings.json` - Local development settings

## Documentation Guidelines

When adding new documentation:

1. **User-facing features** → Update [README.md](../README.md)
2. **Deployment/operations** → Update [AZURE_DEPLOYMENT.md](../AZURE_DEPLOYMENT.md)
3. **Function-specific details** → Create/update `[Function]-README.md` in docs/
4. **M365 bot details** → Create/update `M365-*.md` in docs/
5. **Web app details** → Create/update `Web-*.md` in docs/
6. **Configuration examples** → Create/update `[Component]-*_EXAMPLES.md` in docs/
7. **Major changes** → Create summary doc in docs/ (e.g., `CONTENT_SAFETY_CHANGES_V2.md`)

### Naming Conventions

- **Function docs**: `[FunctionName]-README.md` (e.g., `ChatFunction-README.md`)
- **Component docs**: `[Component]-[Topic].md` (e.g., `M365-CONFIGURATION_GUIDE.md`)
- **Feature docs**: `[Feature].md` or `[Feature]_[Subtopic].md` (e.g., `ContentSafety.md`)
- **Change docs**: `[Feature]_CHANGES.md` or `[Feature]_CHANGES_V2.md`
- **Configuration docs**: `[Topic]_CONFIGURATION.md` or `[Component]_CONFIG_EXAMPLES.md`

## Documentation Structure

All documentation is now centralized in the `/docs` directory, organized by:
- **Azure Functions** - Function-specific documentation with `[Function]-` prefix
- **M365 Bot** - Bot documentation with `M365-` prefix
- **Web App** - Web application documentation with `Web-` prefix
- **Content Safety** - Content Safety feature documentation
- **Reference** - Cross-cutting technical documentation
- **Change History** - Implementation and change summaries

This structure makes it easy to find documentation for specific components.
