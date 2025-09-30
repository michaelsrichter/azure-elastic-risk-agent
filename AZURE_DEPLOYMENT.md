# Azure Deployment Guide

This document provides instructions for deploying the ElasticOn.RiskAgent.Demo.Functions to Azure using Azure Developer CLI (azd).

## Prerequisites

1. **Azure Developer CLI (azd)**: Install from https://aka.ms/azd-install
2. **Azure CLI**: Install from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
3. **.NET 8 SDK**: Required for building the Functions project
4. **Azure Subscription**: With appropriate permissions to create resources

## Getting Started

### 1. Authentication

Login to Azure:

```bash
az login
azd auth login
```

### 2. Setup Environment (Optional)

You can optionally run the setup script to verify your authentication:

```bash
./scripts/setup-azd-environment.sh
```

This script will verify you're logged into Azure CLI and AZD.

**Direct Setup**: Initialize and deploy in one command:

```bash
azd up
```

This will prompt you to create an environment if needed, then deploy everything.

### 3. Deploy to Azure

Deploy infrastructure and application:

```bash
azd up
```

This will create all Azure resources and deploy your Functions app.

This command will:
- Provision Azure resources (Function App, Storage, Key Vault, Application Insights, Azure OpenAI)
- Build and deploy your Functions application
- Configure all necessary permissions and connections

## Architecture

The deployment creates the following Azure resources:

- **Azure Functions App** (Flex Consumption plan)
- **Azure Storage Account** (for Functions runtime)
- **Application Insights** (monitoring and logging)
- **Log Analytics Workspace** (centralized logging)
- **Azure OpenAI Service** (text embeddings)
- **Managed Identity** (secure resource access)

> **Note:** Azure Key Vault is not currently used due to Flex Consumption plan limitations.  
> Secrets are stored directly in Function App settings. See [docs/TODO_KEYVAULT_INTEGRATION.md](docs/TODO_KEYVAULT_INTEGRATION.md) for future plans.

## Configuration

### Elasticsearch Settings (Automated)

After deployment, update your Elasticsearch connection details using the provided script:

```bash
./scripts/update-elasticsearch-secrets.sh
```

This script will:
- Prompt you for your Elasticsearch cluster URI and API key
- Update the settings in the Function App directly
- Restart the Function App to apply changes
- Provide confirmation and next steps

**Manual Alternative**: Update settings via Azure CLI:

```bash
# Get your Function App details
FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME)
RESOURCE_GROUP=$(azd env get-value AZURE_RESOURCE_GROUP)

# Update Elasticsearch settings
az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings \
        "ElasticsearchUri=https://your-elasticsearch-cluster.com:9200" \
        "ElasticsearchApiKey=your-elasticsearch-api-key"
```

The Elasticsearch index name is pre-configured as `risk-agent-documents-v2` but can be modified in the Function App settings if needed.

### User Permissions

The deployment automatically configures managed identity access for:
- **Storage Account**: Blob Data Contributor for Functions runtime
- **Azure OpenAI**: Cognitive Services OpenAI User for embeddings

### Function App Settings

The following environment variables are automatically configured:
- `AzureWebJobsStorage__accountName`: Storage account for Functions
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: Application Insights connection
- `FUNCTIONS_EXTENSION_VERSION`: ~4
- `AZURE_OPENAI_ENDPOINT`: Azure OpenAI service endpoint
- `AZURE_OPENAI_API_KEY`: API key from Azure OpenAI resource
- `AzureOpenAiInferenceId`: Inference endpoint ID for text embeddings (must match Elasticsearch configuration)
- `ElasticsearchUri`: Elasticsearch cluster URL (set via script or manually)
- `ElasticsearchApiKey`: Elasticsearch API key (set via script or manually)
- `ElasticsearchIndexName`: Document index name (risk-agent-documents-v2)
- `ChunkSize`: Text chunk size (500 characters)
- `ChunkOverlap`: Chunk overlap size (50 characters)
- `INTERNAL_FUNCTION_KEY`: For internal function-to-function authentication

## Viewing Logs

Your Functions are configured with comprehensive logging:

### Application Insights (Recommended)
1. Go to Azure Portal → Your Function App → Application Insights
2. Click **Logs** and use queries like:
```kusto
traces
| where timestamp > ago(1h)
| order by timestamp desc
| project timestamp, severityLevel, message
```

### Live Metrics
- Navigate to Application Insights → Live Metrics
- See real-time logs and performance metrics

### Function Monitor
- Go to your Function App → Functions → Select a function → Monitor
- View invocation history and logs per execution

## Testing

Once deployed, you can test the Functions:

1. **Health Check**: Test that the app is running
   ```bash
   FUNCTION_URL=$(azd env get-value AZURE_FUNCTION_APP_URL)
   curl "$FUNCTION_URL/api/health"
   ```

2. **Test Endpoints** (require function key authentication):
   - `POST /api/process-pdf`: Process PDF documents
   - `POST /api/index-document`: Index document chunks

3. **Get Function Key** for authenticated requests:
   ```bash
   FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME)
   FUNCTION_KEY=$(az functionapp keys list --name $FUNCTION_APP_NAME --resource-group $(azd env get-value AZURE_RESOURCE_GROUP) --query "functionKeys.default" -o tsv)
   ```

Use the provided `.http` files for testing:
- `process-pdf.http`
- `index-document.http`

## Monitoring

- **Application Insights**: Monitor performance, logs, and errors
- **Log Analytics**: Query detailed logs and metrics
- **Azure Portal**: View resource health and configuration

## Cleanup

To remove all deployed resources:

```bash
azd down
```

## Example Usage

After successful deployment, you can test your Functions app:

```bash
# Get your Function App URL
FUNCTION_URL=$(azd env get-value AZURE_FUNCTION_APP_URL)

# Test the PDF processing endpoint
curl -X POST "$FUNCTION_URL/api/process-pdf" \
  -H "Content-Type: application/json" \
  -d @samplePdfFileRequest.json

# Test the document indexing endpoint  
curl -X POST "$FUNCTION_URL/api/index-document" \
  -H "Content-Type: application/json" \
  -d '{
    "chunk": "Sample document text chunk",
    "metadata": {
      "id": "test-doc-001", 
      "filenameWithExtension": "test.pdf",
      "pageNumber": 1,
      "chunkNumber": 1
    }
  }'
```

## Troubleshooting

### Common Issues

1. **Deployment Fails**: Check that you have sufficient permissions in the Azure subscription
2. **Function Not Starting**: Verify storage account connection and managed identity permissions
3. **Elasticsearch Connection**: Run `./scripts/update-elasticsearch-secrets.sh` to set correct connection details

### Logs and Diagnostics

- View Function logs in Azure Portal > Function App > Log stream
- Query Application Insights for detailed telemetry
- Check deployment logs with `azd logs`

### Updating Elasticsearch Configuration

If you need to change your Elasticsearch settings after deployment:

```bash
# Use the automated script (recommended)
./scripts/update-elasticsearch-secrets.sh

# Or manually via Azure CLI
FUNCTION_APP_NAME=$(azd env get-value AZURE_FUNCTION_APP_NAME)
RESOURCE_GROUP=$(azd env get-value AZURE_RESOURCE_GROUP)

az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings \
        "ElasticsearchUri=https://new-cluster.com:9200" \
        "ElasticsearchApiKey=new-api-key"

# Restart to apply changes
az functionapp restart --name "$FUNCTION_APP_NAME" --resource-group "$RESOURCE_GROUP"
```

### Updating Azure OpenAI Inference ID

The `AzureOpenAiInferenceId` must match your Elasticsearch inference endpoint:

```bash
# Update the inference ID
az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings "AzureOpenAiInferenceId=your-inference-id-from-elasticsearch"
```

> **Important**: The inference ID format should match what you configured in Elasticsearch (e.g., `azureopenai-text_embedding-xxxxx`)

## Next Steps

1. **Configure Elasticsearch**: Run `./scripts/update-elasticsearch-secrets.sh` to set your Elasticsearch connection
2. **Configure Inference ID**: Update `AzureOpenAiInferenceId` to match your Elasticsearch inference endpoint
3. **Test the deployment**: Use the health check and test endpoints
4. **Set up monitoring**: Configure alerts in Application Insights
5. **Review logs**: Check Application Insights for any startup issues

## Important Notes

### Function-to-Function Authentication
The `ProcessPdfFunction` calls `IndexDocumentFunction` internally. This requires:
- The `INTERNAL_FUNCTION_KEY` app setting (automatically configured)
- The function key is used to authenticate internal calls in Azure
- No authentication needed for local development

If you encounter 401 errors on internal calls, run:
```bash
./scripts/configure-function-key.sh
```

### Elasticsearch Index Configuration
- **Index Name**: `risk-agent-documents-v2` (configurable via app settings)
- **Inference Endpoint**: Must match the `AzureOpenAiInferenceId` setting
- If you change the inference ID, you may need to create a new index with a different name

### Security Considerations
⚠️ **Secrets Storage**: Secrets are currently stored directly in Function App settings (not Key Vault) due to Flex Consumption plan limitations. This means:
- Secrets are visible to users with Function App access in Azure Portal
- No automatic secret rotation
- See [docs/TODO_KEYVAULT_INTEGRATION.md](docs/TODO_KEYVAULT_INTEGRATION.md) for future Key Vault integration plans

## Additional Resources

- **Logging Guide**: Application Insights queries and log levels configured
- **TODO: Key Vault**: [docs/TODO_KEYVAULT_INTEGRATION.md](docs/TODO_KEYVAULT_INTEGRATION.md) - Future integration plan
- **Deployment Summary**: [docs/KEYVAULT_REMOVAL_SUMMARY.md](docs/KEYVAULT_REMOVAL_SUMMARY.md) - Recent changes