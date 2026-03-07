namespace ElasticOn.RiskAgent.Demo.Web.Models;

/// <summary>
/// Represents a chat message in the conversation.
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsThinking { get; set; } = false;

    // Agent metadata (populated for assistant messages)
    public string? AgentName { get; set; }
    public string? AgentId { get; set; }
    public string? AgentCreatedAt { get; set; }
    public string? Model { get; set; }
    public long? ElapsedMs { get; set; }
    public int? RequestLength { get; set; }
    public long? PromptTokens { get; set; }
    public long? CompletionTokens { get; set; }
    public long? TotalTokens { get; set; }
}

/// <summary>
/// Request model for sending a message to the API.
/// </summary>
public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string? ThreadId { get; set; }
    
    /// <summary>
    /// Optional content safety mode override. If not specified, uses configuration default.
    /// Valid values: "Disabled", "Audit", "Enforce"
    /// </summary>
    public string? ContentSafetyMode { get; set; }
    
    /// <summary>
    /// Optional agent ID for the Internal page. If specified, this agent will be used directly.
    /// </summary>
    public string? AgentId { get; set; }
    
    /// <summary>
    /// Optional agent name for the Internal page. If specified, a dynamic agent will be created or reused.
    /// </summary>
    public string? AgentName { get; set; }
    
    /// <summary>
    /// Optional agent instructions for the Internal page dynamic agent.
    /// </summary>
    public string? AgentInstructions { get; set; }
    
    /// <summary>
    /// Optional comma-delimited MCP tool names for the Internal page dynamic agent.
    /// </summary>
    public string? AgentTools { get; set; }
}

/// <summary>
/// Response model from the API after sending a message.
/// </summary>
public class SendMessageResponse
{
    public string Message { get; set; } = string.Empty;
    public string MessageHtml { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public string? AgentName { get; set; }
    public string? AgentId { get; set; }
    public string? AgentCreatedAt { get; set; }
    public string? Model { get; set; }
    public long? ElapsedMs { get; set; }
    public int? RequestLength { get; set; }
    public long? PromptTokens { get; set; }
    public long? CompletionTokens { get; set; }
    public long? TotalTokens { get; set; }
}
