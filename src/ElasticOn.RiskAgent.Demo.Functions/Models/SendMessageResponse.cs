namespace ElasticOn.RiskAgent.Demo.Functions.Models;

/// <summary>
/// Response model for chat messages
/// </summary>
public class SendMessageResponse
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The assistant's response message (markdown format)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The assistant's response message converted to HTML
    /// </summary>
    public string MessageHtml { get; set; } = string.Empty;

    /// <summary>
    /// The thread ID (returned after first message, maintained for subsequent messages)
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? Error { get; set; }
}
