# Chat Function

## Overview

The Chat Function is an HTTP-triggered Azure Function that provides a stateless REST API for the Blazor WebAssembly frontend. It implements the same RiskAgent functionality as the M365 bot, using Azure AI Foundry Agents with Elastic MCP (Model Context Protocol) tools to answer risk-related questions.

## Architecture

The Chat Function mirrors the RiskAgentBot implementation from the M365 project but adapted for a stateless HTTP API:

```
Client (Blazor WASM) → HTTP POST /api/chat → ChatFunction
                                                   ↓
                                          Azure AI Agent Service
                                                   ↓
                                          PersistentAgentsClient
                                                   ↓
                                          Azure AI Foundry
                                                   ↓
                                          Elastic MCP Tools
                                                   ↓
                                          Content Safety Service
```

## Key Components

### Services

1. **AzureAIAgentService** (`Services/AzureAIAgentService.cs`)
   - Manages Azure AI Foundry Agent creation and configuration
   - Handles MCP tool setup with Elastic Search integration
   - Configures authentication headers for Elastic API

2. **ContentSafetyService** (`Services/ContentSafetyService.cs`)
   - Implements jailbreak detection using Azure Content Safety Prompt Shield
   - Supports three modes: Disabled, Audit, Enforce
   - Analyzes both user prompts and MCP tool outputs

3. **ChatStateService** (`Services/ChatStateService.cs`)
   - Manages conversation state (agentId, threadId) per conversationId
   - In-memory implementation (consider Azure Table Storage/Redis for production)
   - Maintains state across multiple HTTP requests within a conversation

### Models

- **ChatMessage** - Represents a chat message with role, content, and timestamp
- **SendMessageRequest** - Request payload with message, conversationId, and optional threadId
- **SendMessageResponse** - Response with success flag, message, threadId, and optional error

### Function Endpoint

**POST** `/api/chat`

**Request Body:**
```json
{
  "message": "What are the key risk factors?",
  "conversationId": "guid-generated-by-client",
  "threadId": "optional-thread-id-from-previous-message"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Based on the documents...",
  "threadId": "thread-id-for-continuation"
}
```

## Configuration

The function requires the following configuration in `local.settings.json`:

```json
{
  "AIServices": {
    "AgentID": "",  // Optional: Pre-configured agent ID
    "ProjectEndpoint": "https://YOUR-PROJECT.services.ai.azure.com/api/projects/YOUR-PROJECT",
    "ModelId": "gpt-4.1-mini",
    "Agent": {
      "Name": "RiskAgent",
      "Instructions": "You answer questions about Risk based on what you find in the context."
    },
    "MCPTool": {
      "ServerLabel": "elastic_search_mcp",
      "ServerUrl": "https://YOUR-CLUSTER.kb.eastus.azure.elastic.cloud/api/agent_builder/mcp",
      "AllowedTools": [
        "azure_elastic_risk_agent.search_docs"
      ]
    },
    "ElasticApiKey": "YOUR_ELASTIC_API_KEY",
    "ContentSafety": {
      "Endpoint": "https://YOUR-CONTENT-SAFETY.cognitiveservices.azure.com/",
      "SubscriptionKey": "YOUR_SUBSCRIPTION_KEY",
      "JailbreakDetectionMode": "Enforce"  // Options: Disabled, Audit, Enforce
    }
  }
}
```

### Environment Variables

Alternatively, you can use environment variables:
- `AZURE_FOUNDRY_PROJECT_ENDPOINT`
- `AZURE_FOUNDRY_PROJECT_MODEL_ID`
- `AZURE_CONTENT_SAFETY_ENDPOINT`
- `AZURE_CONTENT_SAFETY_SUBSCRIPTION_KEY`
- `AZURE_CONTENT_SAFETY_JAILBREAK_DETECTION_MODE`

## Content Safety Modes

### Disabled
- No jailbreak detection performed
- Fastest performance
- Use only in trusted environments

### Audit
- Jailbreak detection runs but doesn't block
- Logs detection events
- Adds warning prefix to responses
- Good for testing and monitoring

### Enforce (Default)
- Blocks requests when jailbreak is detected
- Returns error to client
- Recommended for production

## Conversation Flow

1. **First Message**
   - Client generates a unique `conversationId`
   - Sends message without `threadId`
   - Function creates new agent and thread
   - Returns response with `threadId`

2. **Subsequent Messages**
   - Client includes same `conversationId` and `threadId`
   - Function retrieves existing agent and thread
   - Maintains conversation history

3. **Content Safety Checks**
   - User prompt is analyzed before processing
   - MCP tool outputs are analyzed before returning to user
   - In Enforce mode, blocks at first detection
   - In Audit mode, logs but continues with warning

## Processing Steps

The ChatFunction follows these steps (similar to RiskAgentBot):

1. Parse and validate request
2. Run jailbreak detection on user prompt (if enabled)
3. Get or create agent for conversation
4. Get or create thread for conversation
5. Add user message to thread
6. Create and execute run with MCP tools
7. Poll for completion
8. Extract MCP tool outputs for content safety analysis
9. Run jailbreak detection on tool outputs (if enabled)
10. Retrieve assistant's response from thread
11. Return formatted response to client

## Error Handling

The function handles various error scenarios:
- Invalid requests (400 Bad Request)
- Jailbreak detected in Enforce mode (400 Bad Request)
- Agent run failures (500 Internal Server Error)
- Run timeouts/cancellations (408 Request Timeout)
- Unexpected errors (500 Internal Server Error)

## Differences from M365 RiskAgentBot

1. **Stateless**: Uses ChatStateService instead of Bot Framework's ConversationState
2. **HTTP API**: Direct HTTP requests instead of Bot Framework messaging
3. **No Streaming**: Simple request/response instead of streaming updates
4. **Manual State**: Client manages conversationId, server manages threadId
5. **Simpler Auth**: Anonymous endpoint (add auth as needed)

## Development

### Running Locally

1. Configure `local.settings.json` with your Azure AI Foundry and Elastic endpoints
2. Start Azure Storage Emulator (Azurite)
3. Run the function:
   ```bash
   cd src/ElasticOn.RiskAgent.Demo.Functions
   func start
   ```
4. Test endpoint:
   ```bash
   curl -X POST http://localhost:7071/api/chat \
     -H "Content-Type: application/json" \
     -d '{
       "message": "What are key risk factors?",
       "conversationId": "test-123"
     }'
   ```

### Testing with Blazor Frontend

The Blazor WebAssembly app (ChatComponent.razor) is designed to call this endpoint:
1. Blazor generates `conversationId` on page load
2. Sends messages to `/api/chat`
3. Receives responses with `threadId`
4. Maintains conversation state across messages

## Production Considerations

1. **State Management**: Replace in-memory ChatStateService with Azure Table Storage or Redis
2. **Authentication**: Add Azure AD authentication to the endpoint
3. **CORS**: Configure CORS for your Blazor WASM origin
4. **Rate Limiting**: Implement rate limiting per user/conversation
5. **Monitoring**: Enable Application Insights for logging and telemetry
6. **Scaling**: Consider conversation state cleanup for long-running conversations

## Related Documentation

- [M365 RiskAgentBot](../../ElasticOn.RiskAgent.Demo.M365/Bot/RiskAgentBot.cs)
- [Content Safety Implementation](../../../docs/ContentSafety.md)
- [Elastic MCP Integration](../../../docs/CUSTOM_ELASTICSEARCH_INDEX.md)
