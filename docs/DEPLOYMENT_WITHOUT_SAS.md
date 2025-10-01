# Deploying with SAS Token Restrictions

## Problem

If your organization has Azure policies that prevent SAS token-based storage access, the standard `azd deploy` command will fail with:

```
ERROR: failed deploying service 'api': publishing zip file: deployment failed: 
InaccessibleStorageException: Failed to access storage account for deployment: 
BlobUploadFailedException: Failed to upload blob to storage account: 
Response status code does not indicate success: 403 (This request is not authorized to perform this operation.)
```

This happens because `azd deploy` uses SAS tokens to upload deployment packages to Azure Storage.

## Solution

Use the provided deployment script that bypasses SAS tokens by using Azure CLI's ZipDeploy method with your Azure identity.

### Step 1: Provision Infrastructure

First, provision your Azure resources using `azd provision`:

```bash
azd provision
```

This creates all Azure resources including the Function App, storage account, and configures managed identity.

### Step 2: Deploy Without SAS Tokens

Use the custom deployment script:

```bash
./scripts/deploy-without-sas.sh
```

This script:
1. Builds your Function App project
2. Creates a deployment zip package
3. Deploys using Azure CLI's `az functionapp deployment source config-zip` command
4. Uses your Azure CLI identity (or managed identity) instead of SAS tokens

### Step 3: Configure Post-Deployment

After deployment, run the post-deployment configuration:

```bash
./scripts/configure-function-key.sh
```

## Complete Deployment Workflow

```bash
# 1. Provision infrastructure
azd provision

# 2. Deploy application code (without SAS tokens)
./scripts/deploy-without-sas.sh

# 3. Configure function keys
./scripts/configure-function-key.sh

# 4. Update Elasticsearch secrets
./scripts/update-elasticsearch-secrets.sh
```

## Alternative: Manual ZipDeploy

If you prefer to deploy manually:

```bash
# Build the project
cd src/ElasticOn.RiskAgent.Demo.Functions
dotnet publish -c Release -o ./bin/publish

# Create zip file
cd ./bin/publish
zip -r ../deploy.zip .

# Deploy using Azure CLI
az functionapp deployment source config-zip \
    --name <your-function-app-name> \
    --src ../deploy.zip \
    --build-remote
```

## Why This Works

- **Azure CLI Authentication**: Uses your Azure AD identity instead of SAS tokens
- **ZipDeploy Method**: Directly uploads via the Function App management API
- **No Storage Access**: Bypasses the storage account deployment container entirely
- **Policy Compliant**: Works within organizational security policies

## Runtime Access

Note that the Function App itself uses **Managed Identity** for runtime storage access (not SAS tokens):

- `AzureWebJobsStorage__accountName`: Storage account name
- `AzureWebJobsStorage__credential`: Set to `managedidentity`
- `AzureWebJobsStorage__clientId`: Managed identity client ID

This ensures your application runs securely without any SAS tokens at runtime.

## Troubleshooting

### Error: "AZURE_FUNCTION_APP_NAME not found"

Run `azd provision` first to create the infrastructure and set environment variables.

### Error: "az: command not found"

Install Azure CLI: https://learn.microsoft.com/cli/azure/install-azure-cli

### Error: "This request is not authorized"

Ensure you're logged in to Azure CLI and have the necessary permissions:

```bash
az login
az account show
```

You need at least **Contributor** role on the resource group or **Website Contributor** role on the Function App.
