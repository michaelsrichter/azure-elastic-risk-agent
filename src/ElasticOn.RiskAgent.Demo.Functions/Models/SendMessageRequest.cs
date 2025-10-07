namespace ElasticOn.RiskAgent.Demo.Functions.Models;

/// <summary>
/// Request model for sending a message to the chat API
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// The user's message text
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The conversation ID (unique per session)
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// The thread ID (maintained across messages in a conversation)
    /// </summary>
    public string? ThreadId { get; set; }
}
