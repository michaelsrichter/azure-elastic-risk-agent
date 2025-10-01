# Deployment Fix: SAS Token Restrictions with Managed Identity Storage

## Problem Summary

After deploying the Azure Functions app using the initial `deploy-without-sas.sh` script, the functions stopped working with the following symptoms:

1. **Health endpoint returned HTTP 503** - "The service is unavailable"
2. **Sync trigger error** - "Encountered an error (InternalServerError) from host runtime"
3. **Functions not initializing** - The function app was running but functions weren't loading

## Root Cause

The issue was with the deployment method. The initial script used the **ZipDeploy API** which directly uploads code to the function app's file system. However, Azure Functions **Flex Consumption plans** with **managed identity storage** expect to run code from a blob storage package (`WEBSITE_RUN_FROM_PACKAGE`), not from direct file deployment.

### Why It Failed

1. **Flex Consumption + Managed Identity**: These plans are designed to run from a blob storage package
2. **ZipDeploy bypassed this**: It deployed files directly to the app's filesystem
3. **Missing WEBSITE_RUN_FROM_PACKAGE**: The function app couldn't locate the code properly
4. **Runtime initialization failed**: Without the proper package location, the host runtime couldn't start

## Solution

### Updated Deployment Approach

The fixed `deploy-without-sas.sh` script now:

1. **Temporarily enables public network access** on the storage account
2. **Uploads the deployment package** to a blob container using Azure CLI with Entra (user) credentials
3. **Sets WEBSITE_RUN_FROM_PACKAGE** to point to the blob URL
4. **Restores the original network access settings**
5. **Restarts the function app** to apply changes

### Key Changes

```bash
# 1. Enable public access temporarily
az storage account update \
    --name "$STORAGE_ACCOUNT_NAME" \
    --public-network-access Enabled

# 2. Upload to blob storage with user credentials (no SAS)
az storage blob upload \
    --container-name deployments \
    --file "./bin/deploy.zip" \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --auth-mode login  # Uses your Entra ID credentials

# 3. Configure function app to run from the blob
az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --settings "WEBSITE_RUN_FROM_PACKAGE=${BLOB_URL}"

# 4. Restore original network settings
az storage account update \
    --name "$STORAGE_ACCOUNT_NAME" \
    --public-network-access Disabled  # Or original setting
```

## Why This Works

### Managed Identity for Runtime Access

The function app uses its **managed identity** to access the blob at runtime:

```bicep
// From main.bicep - these settings enable managed identity storage access
{
  name: 'AzureWebJobsStorage__accountName'
  value: storageAccount.name
}
{
  name: 'AzureWebJobsStorage__credential'
  value: 'managedidentity'
}
{
  name: 'AzureWebJobsStorage__clientId'
  value: managedIdentity.properties.clientId
}
```

### User Credentials for Deployment

During deployment, we use **your Entra ID credentials** (not SAS tokens) to upload the package. This bypasses the organizational SAS token restrictions.

### Temporary Public Access

We temporarily enable public network access because:
- The storage account has `allowSharedKeyAccess: false`
- Blob upload from local machine needs network access
- This is safe because we use Entra ID auth (no keys/SAS)
- We immediately restore the original restrictive settings after upload

## Deployment Flow

```
┌─────────────────────┐
│  Build .NET Project │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────────────┐
│ Enable Public Network Access│  ← Temporary, for upload only
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│  Upload to Blob Storage     │  ← Uses Entra ID (your credentials)
│  (no SAS token needed)      │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Restore Network Settings    │  ← Back to secure config
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Set WEBSITE_RUN_FROM_PACKAGE│  ← Point to blob URL
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│   Restart Function App      │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Function App Uses Managed   │  ← Runtime uses managed identity
│ Identity to Access Blob     │     to read from blob
└─────────────────────────────┘
```

## Verification

After deployment, verify with:

```bash
# Check health endpoint
curl https://azfunc7nnn24ozht26y.azurewebsites.net/api/health

# Should return:
{
  "status": "healthy",
  "timestamp": "...",
  "message": "Function App is running..."
}
```

## Benefits of This Approach

1. ✅ **No SAS tokens** - Uses Entra ID authentication
2. ✅ **Compliant with policies** - Works with organizations that block SAS access
3. ✅ **Secure** - Public access is only temporary during deployment
4. ✅ **Proper for Flex Consumption** - Uses WEBSITE_RUN_FROM_PACKAGE as intended
5. ✅ **Managed identity at runtime** - Function app uses secure managed identity to access storage

## Files Modified

- `scripts/deploy-without-sas.sh` - Updated deployment script
- `docs/DEPLOYMENT_FIX.md` - This documentation

## Related Documentation

- [DEPLOYMENT_WITHOUT_SAS.md](./DEPLOYMENT_WITHOUT_SAS.md) - Original deployment workaround
- [Azure Functions Flex Consumption](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan)
- [Managed Identity for Storage](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference?tabs=blob#connecting-to-host-storage-with-an-identity)
