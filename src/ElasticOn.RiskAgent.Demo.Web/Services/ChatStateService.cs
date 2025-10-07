namespace ElasticOn.RiskAgent.Demo.Web.Services;

/// <summary>
/// Service to manage chat state including conversationId and threadId.
/// Similar to RiskAgentBot's conversation state management.
/// </summary>
public class ChatStateService
{
    private string? _conversationId;
    private string? _threadId;

    /// <summary>
    /// Gets or initializes the conversation ID.
    /// Generated once per session when first accessed.
    /// </summary>
    public string ConversationId
    {
        get
        {
            if (string.IsNullOrEmpty(_conversationId))
            {
                _conversationId = Guid.NewGuid().ToString();
            }
            return _conversationId;
        }
    }

    /// <summary>
    /// Gets or sets the thread ID.
    /// Set by the backend after the first message is sent.
    /// </summary>
    public string? ThreadId
    {
        get => _threadId;
        set => _threadId = value;
    }

    /// <summary>
    /// Resets the conversation state, creating a new conversation.
    /// </summary>
    public void ResetConversation()
    {
        _conversationId = Guid.NewGuid().ToString();
        _threadId = null;
    }
}
