targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

// Generate a unique token to be used in naming resources
var resourceToken = uniqueString(subscription().id, resourceGroup().id, location, environmentName)

// Tags to be applied to all resources
var tags = {
  'azd-env-name': environmentName
}

// User-assigned managed identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'azid${resourceToken}'
  location: location
  tags: tags
}

// Storage account for Function App
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'azst${resourceToken}'
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    // Note: allowSharedKeyAccess must be true for azd deploy to work with organizational policies
    // The function app itself uses managed identity for runtime access via AzureWebJobsStorage__credential
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    // Network rules to allow Azure services (required for deployment)
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// Blob service for storage account
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Deployments container for Function App
resource deploymentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'deployments'
  properties: {
    publicAccess: 'None'
  }
}

// Log Analytics workspace for Application Insights
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'azlaw${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights for monitoring
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'azai${resourceToken}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Azure OpenAI Service for text embeddings
resource openAiService 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'azoai${resourceToken}'
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: 'azoai${resourceToken}'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Azure AI Foundry Service
resource aiFoundryService 'Microsoft.CognitiveServices/accounts@2025-07-01-preview' = {
  name: 'azaif${resourceToken}'
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    apiProperties: {}
    customSubDomainName: 'azaif${resourceToken}'
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    allowProjectManagement: true
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o-mini Model Deployment (250K TPM)
resource gpt4oMiniDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-07-01-preview' = {
  parent: aiFoundryService
  name: 'gpt-4o-mini'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
      version: '2024-07-18'
    }
    raiPolicyName: 'Microsoft.Default'
  }
  sku: {
    name: 'Standard'
    capacity: 250
  }
}

// Text Embedding Model Deployment
resource textEmbeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAiService
  name: 'text-embedding-ada-002'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
    raiPolicyName: 'Microsoft.Default'
  }
  sku: {
    name: 'Standard'
    capacity: 120
  }
}

// Azure Static Web App (Standard SKU)
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'azswa${resourceToken}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'web'
  })
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Enabled'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

// App Service Plan for Function App (Flex Consumption)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'azasp${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: 'azfunc${resourceToken}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'api'
  })
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    reserved: true
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
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
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'Recommended'
        }
        {
          name: 'AZURE_OPENAI_ENDPOINT'
          value: openAiService.properties.endpoint
        }
        {
          name: 'AZURE_OPENAI_API_KEY'
          value: openAiService.listKeys().key1
        }
        {
          name: 'AzureOpenAiInferenceId'
          value: 'azureopenai-text-embedding-${resourceToken}'
        }
        {
          name: 'ElasticsearchUri'
          value: 'https://your-elasticsearch-cluster.com:9200'
        }
        {
          name: 'ElasticsearchApiKey'
          value: 'your-elasticsearch-api-key-here'
        }
        {
            name: 'ElasticsearchIndexName'
            value: 'risk-agent-documents-v2'
          }
        {
          name: 'ElasticsearchMaxChunkSize'
          value: '50'
        }
        {
          name: 'ElasticsearchChunkingStrategy'
          value: 'none'
        }
        {
          name: 'ChunkSize'
          value: '500'
        }
        {
          name: 'ChunkOverlap'
          value: '50'
        }
        {
          name: 'INTERNAL_FUNCTION_KEY'
          value: 'placeholder-will-be-updated-after-deployment'
        }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
    clientAffinityEnabled: false
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.properties.primaryEndpoints.blob}deployments'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: managedIdentity.id
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 200
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0'
      }
    }
  }
  dependsOn: [
    storageRoleAssignments
  ]
}

// Note: Function App Configuration is now included directly in the functionApp resource above

// Diagnostic settings for Function App
resource functionAppDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'azfunc-diagnostics'
  scope: functionApp
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'FunctionAppLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

// Role assignments for the managed identity
resource storageRoleAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for role in [
  {
    name: 'Storage Blob Data Owner'
    id: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  }
  {
    name: 'Storage Blob Data Contributor' 
    id: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  }
  {
    name: 'Storage Queue Data Contributor'
    id: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  }
  {
    name: 'Storage Table Data Contributor'
    id: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  }
]: {
  name: guid(storageAccount.id, managedIdentity.id, role.id)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role.id)
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}]

// Note: User storage role assignments can be added manually via Azure Portal or CLI if needed
// Avoided here to prevent role assignment conflicts during deployment

// Monitoring Metrics Publisher role assignment for Application Insights
resource monitoringRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(applicationInsights.id, managedIdentity.id, '3913510d-42f4-4e42-8a64-420c390055eb')
  scope: applicationInsights
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '3913510d-42f4-4e42-8a64-420c390055eb')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Azure OpenAI User role assignment for the managed identity
resource openAiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAiService.id, managedIdentity.id, '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  scope: openAiService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Static Web App Contributor role assignment for the managed identity
resource staticWebAppRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(staticWebApp.id, managedIdentity.id, 'b24988ac-6180-42a0-ab88-20f7382dd24c')
  scope: staticWebApp
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output RESOURCE_GROUP_ID string = resourceGroup().id
output AZURE_FUNCTION_APP_NAME string = functionApp.name
output AZURE_FUNCTION_APP_URL string = 'https://${functionApp.properties.defaultHostName}'
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
output AZURE_STORAGE_ACCOUNT_NAME string = storageAccount.name
output AZURE_OPENAI_ENDPOINT string = openAiService.properties.endpoint
output AZURE_OPENAI_DEPLOYMENT_NAME string = textEmbeddingDeployment.name
output AZURE_OPENAI_INFERENCE_ID string = 'azureopenai-text-embedding-${resourceToken}'
output AZURE_AI_FOUNDRY_NAME string = aiFoundryService.name
output AZURE_AI_FOUNDRY_ENDPOINT string = aiFoundryService.properties.endpoint
output AZURE_AI_FOUNDRY_GPT4O_MINI_DEPLOYMENT_NAME string = gpt4oMiniDeployment.name
output AZURE_STATIC_WEB_APP_NAME string = staticWebApp.name
output AZURE_STATIC_WEB_APP_URL string = 'https://${staticWebApp.properties.defaultHostname}'


