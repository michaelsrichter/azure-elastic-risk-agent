# Contoso Risk Agent Knowledgebase (CRAK) - Blazor WebAssembly Chat

This Blazor WebAssembly application provides a sleek chat interface for risk analysis powered by Azure Functions and Microsoft Agent Framework.

## What's Been Done

### 1. **Chat State Management**
- Created `ChatStateService` to manage `conversationId` and `threadId` similar to RiskAgentBot
- State persists across the session
- Conversation can be reset if needed

### 2. **Chat Models**
- `ChatMessage`: Represents individual messages with role (user/assistant), content, and timestamp
- `SendMessageRequest`: Request payload sent to the Azure Function API
- `SendMessageResponse`: Response from the API including the thread ID

### 3. **Chat Component**
- Full-featured chat interface with:
  - Welcome screen on first load
  - Message history display
  - Thinking indicator while processing
  - Real-time message streaming (ready for implementation)
  - Keyboard support (Enter to send)
  - Disabled state while processing

### 4. **UI/UX Updates**
- Removed Weather and Counter sample pages
- Simplified navigation (single-page app)
- Modern gradient design with purple theme
- Smooth animations and transitions
- Responsive message bubbles
- Professional branding as "CRAK"

### 5. **Configuration**
- HttpClient configured to call Azure Function API
- Development config points to `http://localhost:7071`
- Production config placeholder for Azure deployment
- ChatStateService registered as singleton

## What's Needed Next

### Backend API (Azure Function)

Create an Azure Function HTTP endpoint at `/api/chat` that:

1. **Accepts** `SendMessageRequest`:
   ```json
   {
     "message": "string",
     "conversationId": "string",
     "threadId": "string?" 
   }
   ```

2. **Uses Microsoft Agent Framework**:
   - Similar to RiskAgentBot implementation
   - Create/retrieve agent based on conversationId
   - Create/retrieve thread based on threadId
   - Run agent with MCP tools (Elastic search)
   - Handle content safety checks
   - Stream response back

3. **Returns** `SendMessageResponse`:
   ```json
   {
     "message": "string",
     "conversationId": "string",
     "threadId": "string",
     "success": true,
     "error": null
   }
   ```

### Example Azure Function Structure

```csharp
[Function("Chat")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "chat")] 
    HttpRequestData req)
{
    var request = await req.ReadFromJsonAsync<SendMessageRequest>();
    
    // Use Microsoft Agent Framework similar to RiskAgentBot
    // - Get/create agent
    // - Get/create thread
    // - Run agent with MCP tools
    // - Return response
    
    var response = new SendMessageResponse
    {
        Message = agentResponse,
        ConversationId = request.ConversationId,
        ThreadId = threadId,
        Success = true
    };
    
    return await req.CreateJsonResponseAsync(response);
}
```

## Configuration

### Development
- API endpoint: `http://localhost:7071` (local Azure Functions)
- Update in `wwwroot/appsettings.Development.json`

### Production
- Update `wwwroot/appsettings.json` with deployed Function App URL
- Example: `https://your-function-app.azurewebsites.net`

## Running the App

```bash
cd src/ElasticOn.RiskAgent.Demo.Web
dotnet run
```

Or for Azure Static Web Apps:
```bash
swa start
```

## Next Steps

1. ✅ Create Azure Function with `/api/chat` endpoint
2. ✅ Integrate Microsoft Agent Framework in the Function
3. ✅ Configure MCP tools for Elastic search
4. ✅ Add content safety integration
5. ✅ Test end-to-end conversation flow
6. ✅ Deploy to Azure Static Web Apps + Function Apps

## Architecture

```
┌─────────────────┐
│  Blazor WASM    │
│  Chat UI        │
└────────┬────────┘
         │ HTTP POST /api/chat
         ▼
┌─────────────────┐
│ Azure Function  │
│  Chat Endpoint  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Microsoft Agent │
│   Framework     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  MCP Tools      │
│ (Elastic Search)│
└─────────────────┘
```
