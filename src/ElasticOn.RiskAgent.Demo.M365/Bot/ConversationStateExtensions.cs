using Microsoft.Agents.Builder.State;

namespace ElasticOn.RiskAgent.Demo.M365.Bot;

public static class ConversationStateExtensions
{
    private const string AgentIdKey = "agentIdKey";
    private const string MessageCountKey = "countKey";
    private const string ConversationThreadKey = "conversationThreadKey";

    public static string? AgentId(this ConversationState state) => state.GetValue<string>(AgentIdKey);

    public static void AgentId(this ConversationState state, string value) => state.SetValue(AgentIdKey, value);

    public static string? SerializedThread(this ConversationState state) => state.GetValue<string>(ConversationThreadKey);

    public static void SerializedThread(this ConversationState state, string value) => state.SetValue(ConversationThreadKey, value);

    public static int MessageCount(this ConversationState state) => state.GetValue<int>(MessageCountKey);

    public static void MessageCount(this ConversationState state, int value) => state.SetValue(MessageCountKey, value);

    public static int IncrementMessageCount(this ConversationState state)
    {
        int count = state.GetValue<int>(MessageCountKey);
        state.SetValue(MessageCountKey, ++count);
        return count;
    }
}