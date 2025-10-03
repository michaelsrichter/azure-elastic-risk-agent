# Azure AI Foundry Agent Integration

This document describes the integration of Azure AI Foundry Agent into the RiskAgentBot using the Microsoft Agent Framework.

## Overview

The RiskAgentBot has been configured to use an Azure AI Foundry Agent through the `PersistentAgentsClient` from the `Azure.AI.Agents.Persistent` package. This integration allows the bot to leverage advanced AI capabilities for processing user messages.

## Architecture

### Components

1. **IAzureAIAgentService** (`Services/IAzureAIAgentService.cs`)
   - Interface for accessing the Azure AI Foundry Agent client
   - Provides methods to get the client and agent ID

2. **AzureAIAgentService** (`Services/AzureAIAgentService.cs`)
   - Implementation of the Azure AI Agent service
   - Initializes `PersistentAgentsClient` using configuration from `appsettings.json`
   - Uses `DefaultAzureCredential` for authentication
   - Configured as a singleton service for the application lifetime

3. **RiskAgentBot** (`Bot/RiskAgentBot.cs`)
   - Main bot implementation that inherits from `AgentApplication`
   - Receives the Azure AI Agent service via dependency injection
   - Maintains conversation thread mappings
   - Processes user messages using the Azure AI Foundry Agent

## Configuration

### appsettings.json

The following configuration is required in `appsettings.json`:

```json
{
  "AIServices": {
    "AgentID": "asst_AZxAQqgUkkk7wNqAipgkRX5I",
    "ProjectEndpoint": "https://risk-agent-aif.services.ai.azure.com/api/projects/firstProject"
  }
}
```

- **AgentID**: The ID of your Azure AI Foundry Agent (obtained from Azure AI Foundry portal)
- **ProjectEndpoint**: The endpoint URL for your Azure AI Foundry project

### Authentication

The service uses `DefaultAzureCredential` which automatically handles authentication through:
- Managed Identity (when deployed to Azure)
- Azure CLI (for local development)
- Visual Studio authentication
- Environment variables
- And other credential sources

For local development, ensure you're logged in via Azure CLI:
```bash
az login
```

## Service Registration

The Azure AI Agent service is registered in `Program.cs`:

```csharp
// Register Azure AI Agent Service
builder.Services.AddSingleton<IAzureAIAgentService, AzureAIAgentService>();

// Add the bot (which is transient)
builder.AddAgent<RiskAgentBot>();
```

The service is registered as a singleton because:
- The `PersistentAgentsClient` is thread-safe
- Configuration doesn't change during the application lifetime
- Reduces overhead of creating new clients for each request

## Bot Implementation

### Message Processing Flow

1. User sends a message to the bot
2. `OnMessageAsync` is triggered
3. Bot retrieves or creates a thread ID for the conversation
4. Bot sends a typing indicator
5. Bot uses the `PersistentAgentsClient` to process the message with the Azure AI Foundry Agent
6. Bot sends the agent's response back to the user

### Conversation Threading

The bot maintains a dictionary mapping conversation IDs to thread IDs:
```csharp
private readonly Dictionary<string, string> _conversationThreads = new();
```

This allows the bot to maintain context across multiple messages in the same conversation.

## Integration with Microsoft Agent Framework

This implementation follows the Microsoft Agent Framework patterns:

- **AgentApplication**: Base class for creating agents
- **ITurnContext**: Provides context for the current turn of conversation
- **ITurnState**: Maintains state across conversation turns
- **Dependency Injection**: Services are injected via constructor
- **Activity Handlers**: OnConversationUpdate and OnActivity for handling events

## Next Steps

### TODO: Full Agent Execution

The current implementation establishes the foundation with all necessary services and configuration. The `PersistentAgentsClient` is initialized but the API methods need to be used according to the actual package implementation.

**Current Status:**
- ? `IAzureAIAgentService` interface and implementation created
- ? `PersistentAgentsClient` initialized with DefaultAzureCredential
- ? Agent ID configured from appsettings.json
- ? Service registered in DI container
- ? RiskAgentBot receives the service via constructor injection
- ?? Agent execution logic needs completion based on actual API

**To Complete the Integration:**

1. **Determine the Correct API**:
   - The `PersistentAgentsClient` from `Azure.AI.Agents.Persistent` may have a different API than documented
   - Check the actual package documentation or use IntelliSense to find available methods
   - The Microsoft.Agents.AI.AzureAI package may provide additional helper classes

2. **Possible Approaches**:

   **Option A: Use PersistentAgentsClient directly** (if methods are available):
   ```csharp
   // Check what methods are actually available on _persistentClient
   // Look for methods like:
   // - CreateThread / CreateThreadAsync
   // - AddMessage / CreateMessageAsync  
   // - Run / CreateRunAsync
   // - GetMessages / GetMessagesAsync
   ```

   **Option B: Use Microsoft.Agents.AI.AzureAI helpers**:
   ```csharp
   // The package may provide wrapper classes like:
   // - AzureAIAgent
   // - PersistentAgentExecutor
   // - AgentRunner
   // Check package namespace for available helpers
   ```

   **Option C: Use the Agent Framework's Execution Pattern**:
   ```csharp
   // May need to configure the agent in AgentApplicationOptions
   // Or use specific interfaces from Microsoft.Agents.AI
   ```

3. **Implement Agent Execution**:
   - Create or retrieve agent threads per conversation
   - Send user messages to the agent
   - Poll or stream agent responses
   - Handle function calling if the agent uses tools
   - Extract and return responses to the user

4. **Enhance Thread Management**:
   - Store thread IDs in bot state storage (`ITurnState`)
   - Implement thread lifecycle management
   - Handle thread expiration and cleanup

5. **Error Handling**:
   - Add retry logic for transient failures
   - Handle agent timeout scenarios
   - Provide fallback responses when agent is unavailable

6. **Testing**:
   - Create unit tests for the Azure AI Agent service
   - Test conversation threading and context maintenance
   - Validate error handling scenarios

## Investigating the API

To find the correct API methods, try these approaches:

1. **IntelliSense Exploration**: Open `RiskAgentBot.cs` and type `_azureAIAgentService.GetClient().` to see available methods

2. **Check Package Source**: Look at the NuGet package details:
   - `Azure.AI.Agents.Persistent` - For PersistentAgentsClient
   - `Microsoft.Agents.AI.AzureAI` - For Azure AI-specific helpers

3. **Review Examples**: Check the Microsoft Agent Framework samples repository for working examples

4. **API Documentation**: Review the specific version of the packages being used:
   - `Microsoft.Agents.AI.AzureAI`: Version 1.0.0-preview.251002.1
   - See what interfaces and classes are exported

## References

- [Azure AI Foundry Agent Documentation](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/azure-ai-foundry-agent)
- [Microsoft Agent Framework Documentation](https://microsoft.github.io/copilot-camp/pages/custom-engine/agents-sdk/)
- [Agent Configuration Guide](https://microsoft.github.io/copilot-camp/pages/custom-engine/agents-sdk/03-agent-configuration/)
- [Run Agent Tutorial](https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/run-agent?pivots=programming-language-csharp)

## Security Considerations

1. **Credentials**: Never commit actual agent IDs or endpoints to source control
2. **Configuration**: Use user secrets for local development, Azure Key Vault for production
3. **Authentication**: Ensure proper Azure RBAC roles are assigned for the bot's identity
4. **Logging**: Be careful not to log sensitive user data or API keys

## Support

For issues or questions about the integration:
- Check the Microsoft Agent Framework documentation
- Review Azure AI Foundry Agent documentation
- Consult the Azure AI Foundry portal for agent configuration
