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

    /// <summary>
    /// Agent name used for this request
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Agent ID used for this request
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// When the agent was created (ISO 8601)
    /// </summary>
    public string? AgentCreatedAt { get; set; }

    /// <summary>
    /// Model used for this request
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Elapsed time in milliseconds for the agent run
    /// </summary>
    public long? ElapsedMs { get; set; }

    /// <summary>
    /// Length of the user message in characters
    /// </summary>
    public int? RequestLength { get; set; }

    /// <summary>
    /// Prompt (input) tokens used
    /// </summary>
    public long? PromptTokens { get; set; }

    /// <summary>
    /// Completion (output) tokens used
    /// </summary>
    public long? CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used
    /// </summary>
    public long? TotalTokens { get; set; }
}
