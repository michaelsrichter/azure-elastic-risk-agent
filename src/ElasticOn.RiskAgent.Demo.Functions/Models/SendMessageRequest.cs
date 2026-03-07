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
