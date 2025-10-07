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
}
