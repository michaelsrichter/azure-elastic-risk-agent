using Azure.AI.Agents.Persistent;
using ElasticOn.RiskAgent.Demo.M365.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace ElasticOn.RiskAgent.Demo.M365.Bot;

public class RiskAgentBot : AgentApplication
{
    private readonly IAzureAIAgentService _azureAIAgentService;
    private readonly ILogger<RiskAgentBot> _logger;
    private readonly PersistentAgentsClient _client;
    private readonly string _agentId;

    public RiskAgentBot(
        AgentApplicationOptions options,
        IAzureAIAgentService azureAIAgentService,
        ILogger<RiskAgentBot> logger) : base(options)
    {
        _azureAIAgentService = azureAIAgentService ?? throw new ArgumentNullException(nameof(azureAIAgentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Get agent configuration
        _agentId = _azureAIAgentService.GetAgentId();
        _client = _azureAIAgentService.GetClient();

        _logger.LogInformation("RiskAgentBot initialized with Agent ID: {AgentId}", _agentId);

        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);

        // Listen for ANY message to be received. MUST BE AFTER ANY OTHER MESSAGE HANDLERS
        OnActivity(ActivityTypes.Message, OnMessageAsync);
    }

    protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Hello and Welcome!"), cancellationToken);
            }
        }
    }

    protected async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        try
        {
            // Send initial working message to user
            await turnContext.StreamingResponse.QueueInformativeUpdateAsync("Working on it...", cancellationToken);

            var userMessage = turnContext.Activity.Text;
            var conversationId = turnContext.Activity.Conversation.Id;

            _logger.LogInformation("Processing message from user in conversation {ConversationId}: {Message}",
                conversationId, userMessage);

            // Get the AI Agent instance
            var agent = await _client.GetAIAgentAsync(_agentId);
            _logger.LogInformation("Retrieved AI Agent with ID: {AgentId} for conversation {ConversationId}", _agentId, conversationId);

            // Send typing indicator to show the bot is processing
            await turnContext.SendActivitiesAsync([new Activity { Type = ActivityTypes.Typing }], cancellationToken);

            // Increment message count in state and queue the count to the user
            int count = turnState.Conversation.IncrementMessageCount();
            turnContext.StreamingResponse.QueueTextChunk($"({count}) ");

            // Create a chat message with the user's input
            var chatMessage = new ChatMessage(ChatRole.User, [new TextContent(userMessage)]);

            _logger.LogInformation("Running AI Agent with streaming for conversation {ConversationId}", conversationId);

            // Stream the agent's response back to the user
            // Pass null for thread to let the agent use the conversation ID for thread management
            await foreach (var messageChunk in agent.RunStreamingAsync([chatMessage], null, null, cancellationToken))
            {
                if (!string.IsNullOrEmpty(messageChunk.Text))
                {
                    turnContext.StreamingResponse.QueueTextChunk(messageChunk.Text);
                }
            }

            _logger.LogInformation("AI Agent completed streaming response for conversation {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in RiskAgentBot for conversation {ConversationId}",
                turnContext.Activity.Conversation.Id);
            await turnContext.SendActivityAsync("Sorry, I encountered an error processing your message. Please try again later.", cancellationToken: cancellationToken);
        }
        finally
        {
            // Indicate that processing is complete
            await turnContext.StreamingResponse.EndStreamAsync(cancellationToken);
        }
    }
}

public static class ConversationStateExtensions
{
    public static int MessageCount(this ConversationState state) => state.GetValue<int>("countKey");

    public static void MessageCount(this ConversationState state, int value) => state.SetValue("countKey", value);

    public static int IncrementMessageCount(this ConversationState state)
    {
        int count = state.GetValue<int>("countKey");
        state.SetValue("countKey", ++count);
        return count;
    }
}