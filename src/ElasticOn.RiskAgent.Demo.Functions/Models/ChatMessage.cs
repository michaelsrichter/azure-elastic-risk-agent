namespace ElasticOn.RiskAgent.Demo.Functions.Models;

/// <summary>
/// Represents a chat message in the conversation
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The role of the message sender (user or assistant)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Whether this is a thinking/processing indicator message
    /// </summary>
    public bool IsThinking { get; set; }
}
