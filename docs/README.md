# Documentation Index

Quick reference to all documentation in this repository.

## Main Documentation

### [README.md](../README.md)
**Start here!** Overview of the project, features, and quick start guide.
- Project overview and architecture
- Prerequisites and setup
- Quick deployment with `azd up`
- What gets deployed to Azure
- Next steps

### [AZURE_DEPLOYMENT.md](../AZURE_DEPLOYMENT.md)
Complete deployment guide for Azure.
- Step-by-step deployment instructions
- Architecture overview
- Configuration management (Elasticsearch, Azure OpenAI)
- Viewing logs in Application Insights
- Testing deployed functions
- Troubleshooting common issues
- Important notes on authentication and security

## Function Documentation

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

## Reference Documentation

### [TODO_KEYVAULT_INTEGRATION.md](./TODO_KEYVAULT_INTEGRATION.md)
Future work: Azure Key Vault integration.
- Why Key Vault was removed
- Current secret management approach
- Security considerations
- Implementation plan for when Flex Consumption supports Key Vault
- Alternative approaches

### [KEYVAULT_REMOVAL_SUMMARY.md](./KEYVAULT_REMOVAL_SUMMARY.md)
Change summary: Key Vault removal.
- What changed and why
- Infrastructure updates
- Code changes
- Testing results
- Migration path for future

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
1. **User-facing features** → Update README.md
2. **Deployment/operations** → Update AZURE_DEPLOYMENT.md
3. **Function-specific details** → Create/update function README in docs/
4. **Future work/TODOs** → Create TODO_*.md in docs/
5. **Major changes** → Create summary doc in docs/ (can be removed after next release)

## Removed Documentation

The following docs have been consolidated and removed:
- ~~ENHANCED_LOGGING.md~~ → Consolidated into AZURE_DEPLOYMENT.md
- ~~FUNCTION_AUTH_QUICKSTART.md~~ → Consolidated into AZURE_DEPLOYMENT.md
- ~~FUNCTION_AUTHENTICATION.md~~ → Consolidated into AZURE_DEPLOYMENT.md
- ~~LOGGING_GUIDE.md~~ → Consolidated into AZURE_DEPLOYMENT.md
- ~~KEYVAULT_FIX.md~~ → Superseded by KEYVAULT_REMOVAL_SUMMARY.md
- ~~KEYVAULT_WORKAROUND_SUMMARY.md~~ → Superseded by KEYVAULT_REMOVAL_SUMMARY.md
- ~~DEPLOYMENT_COMPLETE.md~~ → Superseded by AZURE_DEPLOYMENT.md
