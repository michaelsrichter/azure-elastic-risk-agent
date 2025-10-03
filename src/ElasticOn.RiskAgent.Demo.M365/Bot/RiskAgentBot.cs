using System;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ElasticOn.RiskAgent.Demo.M365.Services;

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
            var userMessage = turnContext.Activity.Text;
            var conversationId = turnContext.Activity.Conversation.Id;
            
            _logger.LogInformation("Processing message from user in conversation {ConversationId}: {Message}", 
                conversationId, userMessage);

            // Send typing indicator to show the bot is processing
            await turnContext.SendActivitiesAsync(new Activity[] { new Activity { Type = ActivityTypes.Typing } }, cancellationToken);

            // Get the AI Agent instance
            _logger.LogInformation("Retrieving AI Agent with ID: {AgentId} for conversation {ConversationId}", _agentId, conversationId);
            var agent = await _client.GetAIAgentAsync(_agentId);

            // Create a chat message with the user's input
            var chatMessage = new ChatMessage(ChatRole.User, userMessage);
            
            _logger.LogInformation("Running AI Agent with user message in conversation {ConversationId}", conversationId);
            
            // Run the agent with the user's message
            var agentResponse = await agent.RunAsync(chatMessage);
            
            _logger.LogInformation("AI Agent returned response for conversation {ConversationId}", conversationId);

            // Send the agent's response back to the user
            var responseText = agentResponse?.ToString();
            if (!string.IsNullOrEmpty(responseText))
            {
                await turnContext.SendActivityAsync(responseText, cancellationToken: cancellationToken);
            }
            else
            {
                _logger.LogWarning("AI Agent returned empty response for conversation {ConversationId}", conversationId);
                await turnContext.SendActivityAsync("I processed your message but didn't generate a response.", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in RiskAgentBot for conversation {ConversationId}", 
                turnContext.Activity.Conversation.Id);
            await turnContext.SendActivityAsync("Sorry, I encountered an error processing your message. Please try again later.", cancellationToken: cancellationToken);
        }
    }
}
