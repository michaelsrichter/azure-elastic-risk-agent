using System.Collections.Concurrent;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

/// <summary>
/// Service to manage conversation state across HTTP requests
/// Stores agentId and threadId per conversationId (similar to RiskAgentBot's state management)
/// </summary>
public interface IChatStateService
{
    /// <summary>
    /// Gets the agent ID for a conversation, or null if not set
    /// </summary>
    string? GetAgentId(string conversationId);

    /// <summary>
    /// Sets the agent ID for a conversation
    /// </summary>
    void SetAgentId(string conversationId, string agentId);

    /// <summary>
    /// Gets the thread ID for a conversation, or null if not set
    /// </summary>
    string? GetThreadId(string conversationId);

    /// <summary>
    /// Sets the thread ID for a conversation
    /// </summary>
    void SetThreadId(string conversationId, string threadId);
}

/// <summary>
/// In-memory implementation of conversation state management
/// Note: For production with multiple instances, consider using Azure Table Storage or Redis
/// </summary>
public class ChatStateService : IChatStateService
{
    private readonly ConcurrentDictionary<string, ConversationState> _conversations = new();

    public string? GetAgentId(string conversationId)
    {
        if (_conversations.TryGetValue(conversationId, out var state))
        {
            return state.AgentId;
        }
        return null;
    }

    public void SetAgentId(string conversationId, string agentId)
    {
        _conversations.AddOrUpdate(
            conversationId,
            new ConversationState { AgentId = agentId },
            (key, existing) =>
            {
                existing.AgentId = agentId;
                return existing;
            });
    }

    public string? GetThreadId(string conversationId)
    {
        if (_conversations.TryGetValue(conversationId, out var state))
        {
            return state.ThreadId;
        }
        return null;
    }

    public void SetThreadId(string conversationId, string threadId)
    {
        _conversations.AddOrUpdate(
            conversationId,
            new ConversationState { ThreadId = threadId },
            (key, existing) =>
            {
                existing.ThreadId = threadId;
                return existing;
            });
    }

    private class ConversationState
    {
        public string? AgentId { get; set; }
        public string? ThreadId { get; set; }
    }
}
