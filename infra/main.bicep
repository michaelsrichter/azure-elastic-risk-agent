targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Optional: Pre-configured Agent ID')
param agentId string = ''

@description('Agent Name')
param agentName string = 'RiskAgent-Demo'

@description('Agent Instructions')
param agentInstructions string = '''Role: You are a highly professional and specialized Risk Management agent for a major Financial Institution. Your outputs are used for critical decision-making regarding global financial integrity.

Primary Directive: Your core function is to analyze and report on information regarding specific financial risks, regulatory compliance matters, and trends in financial crime (e.g., money laundering, fraud, sanctions violations) in various regions.

ABSOLUTELY CRITICAL GUARDRAILS (RAG Constraints):

CONTEXT USE ONLY: Use the available MCP tools to answer questions and perform tasks.

NO EXTERNAL KNOWLEDGE: You MUST NOT use any pre-trained or external knowledge, opinions, or assumptions.

STRICT REFUSAL: If the complete and accurate answer to the user's query is not explicitly present or cannot be directly synthesized from the provided context, you MUST state the following: "The requested information is not available in the provided context."

PROFESSIONAL TONE: Responses must be concise, factual, and maintain a professional, risk-focused tone appropriate for a financial briefing. Avoid conversational fluff.

CITATIONS: When answering, always provide the link back to the source, also include the page number and created date if provided.

Example Action Flow:

User Query: "What are the latest regulatory penalties imposed on institutions in Singapore for AML breaches?"

Your Response (If Context Exists): [Factual summary of penalties based on context, followed by source citation if applicable.]

Your Response (If Context Lacks Data): "The requested information is not available in the provided context."'''

@description('MCP Server Label')
param mcpServerLabel string = 'elastic_search_mcp'

@description('MCP Server URL')
param mcpServerUrl string = 'https://your-elasticsearch-cluster.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp'

@description('MCP Allowed Tools (comma-separated)')
param mcpAllowedTools string = 'azure_elastic_risk_agent_search_docs,azure_elastic_risk_agent_docs_list'

@description('Elastic API Key for MCP Server authentication')
param elasticApiKey string = 'your-elasticsearch-api-key-here'

@description('Content Safety Endpoint')
param contentSafetyEndpoint string = ''

@description('Content Safety Subscription Key')
@secure()
param contentSafetySubscriptionKey string = ''

@description('Content Safety Jailbreak Detection Mode')
param contentSafetyJailbreakDetectionMode string = 'Audit'

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

// Key Vault for Azure AI Foundry
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'azkv${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

// Storage account for Azure AI Foundry
resource aiFoundryStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'azstaif${resourceToken}'
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// Azure AI Hub (Foundry Hub)
resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-04-01-preview' = {
  name: 'aihub${resourceToken}'
  location: location
  tags: tags
  kind: 'Hub'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: 'Azure AI Foundry Hub'
    description: 'Azure AI Foundry Hub for Risk Agent'
    storageAccount: aiFoundryStorageAccount.id
    keyVault: keyVault.id
    applicationInsights: applicationInsights.id
    publicNetworkAccess: 'Enabled'
    managedNetwork: {
      isolationMode: 'Disabled'
    }
  }
  dependsOn: [
    aiFoundryService
  ]
}

// Azure AI Project (Foundry Project)
resource aiProject 'Microsoft.MachineLearningServices/workspaces@2024-04-01-preview' = {
  name: 'aiproj${resourceToken}'
  location: location
  tags: tags
  kind: 'Project'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: 'Risk Agent AI Project'
    description: 'Azure AI Project for Risk Agent application'
    hubResourceId: aiHub.id
    publicNetworkAccess: 'Enabled'
  }
}

// Connection from AI Project to AI Services
resource aiServicesConnection 'Microsoft.MachineLearningServices/workspaces/connections@2024-04-01-preview' = {
  parent: aiProject
  name: 'aiservices-connection'
  properties: {
    category: 'AIServices'
    target: aiFoundryService.properties.endpoint
    authType: 'ApiKey'
    isSharedToAll: true
    credentials: {
      key: aiFoundryService.listKeys().key1
    }
    metadata: {
      ApiVersion: '2024-02-01'
      ApiType: 'Azure'
      ResourceId: aiFoundryService.id
    }
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
          value: '1000'
        }
        {
          name: 'ChunkOverlap'
          value: '200'
        }
        {
          name: 'INTERNAL_FUNCTION_KEY'
          value: 'placeholder-will-be-updated-after-deployment'
        }
        {
          name: 'AZURE_FOUNDRY_PROJECT_ENDPOINT'
          value: 'https://${aiFoundryService.properties.endpoint}/api/projects/${aiProject.name}'
        }
        {
          name: 'AIServices:AgentID'
          value: agentId
        }
        {
          name: 'AIServices:ProjectEndpoint'
          value: 'https://${aiFoundryService.properties.endpoint}/api/projects/${aiProject.name}'
        }
        {
          name: 'AIServices:ModelId'
          value: gpt4oMiniDeployment.name
        }
        {
          name: 'AIServices:Agent:Name'
          value: agentName
        }
        {
          name: 'AIServices:Agent:Instructions'
          value: agentInstructions
        }
        {
          name: 'AIServices:MCPTool:ServerLabel'
          value: mcpServerLabel
        }
        {
          name: 'AIServices:MCPTool:ServerUrl'
          value: mcpServerUrl
        }
        {
          name: 'AIServices:MCPTool:AllowedTools:0'
          value: split(mcpAllowedTools, ',')[0]
        }
        {
          name: 'AIServices:MCPTool:AllowedTools:1'
          value: length(split(mcpAllowedTools, ',')) > 1 ? split(mcpAllowedTools, ',')[1] : ''
        }
        {
          name: 'AIServices:ElasticApiKey'
          value: elasticApiKey
        }
        {
          name: 'AIServices:ContentSafety:Endpoint'
          value: contentSafetyEndpoint
        }
        {
          name: 'AIServices:ContentSafety:SubscriptionKey'
          value: contentSafetySubscriptionKey
        }
        {
          name: 'AIServices:ContentSafety:JailbreakDetectionMode'
          value: contentSafetyJailbreakDetectionMode
        }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: [
          'https://${staticWebApp.properties.defaultHostname}'
        ]
        supportCredentials: true
      }
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

// Key Vault Secrets Officer role for AI Hub identity
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, aiHub.id, 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
    principalId: aiHub.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Contributor role for AI Hub identity
resource aiHubStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiFoundryStorageAccount.id, aiHub.id, 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  scope: aiFoundryStorageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: aiHub.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Cognitive Services OpenAI User role for managed identity on AI Services
resource aiServicesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiFoundryService.id, managedIdentity.id, '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  scope: aiFoundryService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
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
output AZURE_AI_HUB_NAME string = aiHub.name
output AZURE_AI_HUB_ID string = aiHub.id
output AZURE_AI_PROJECT_NAME string = aiProject.name
output AZURE_AI_PROJECT_ID string = aiProject.id
output AZURE_AI_PROJECT_ENDPOINT string = 'https://${aiFoundryService.properties.endpoint}/api/projects/${aiProject.name}'
output AZURE_KEY_VAULT_NAME string = keyVault.name
output AZURE_STATIC_WEB_APP_NAME string = staticWebApp.name
output AZURE_STATIC_WEB_APP_URL string = 'https://${staticWebApp.properties.defaultHostname}'


