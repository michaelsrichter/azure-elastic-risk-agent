using System.Text.Json;
using Azure.AI.Agents.Persistent;
using ElasticOn.RiskAgent.Demo.M365.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace ElasticOn.RiskAgent.Demo.M365.Bot;

/// <summary>
/// Bot implementation that uses Azure AI Foundry Agents with Elastic MCP tools to answer risk-related questions.
/// Maintains conversation state and handles message streaming back to users.
/// </summary>
public class RiskAgentBot : AgentApplication
{
    #region Fields

    private readonly IAzureAIAgentService _azureAIAgentService;
    private readonly ILogger<RiskAgentBot> _logger;
    private readonly PersistentAgentsClient _client;

    #endregion

    #region Constructor

    public RiskAgentBot(
        AgentApplicationOptions options,
        IAzureAIAgentService azureAIAgentService,
        ILogger<RiskAgentBot> logger) : base(options)
    {
        _azureAIAgentService = azureAIAgentService ?? throw new ArgumentNullException(nameof(azureAIAgentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Get the Azure AI Foundry client for managing agents, threads, and runs
        _client = _azureAIAgentService.GetClient();

        _logger.LogInformation("RiskAgentBot initialized");

        // Register event handler for when new members join the conversation
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);

        // Register message handler - MUST BE AFTER ANY OTHER MESSAGE HANDLERS
        OnActivity(ActivityTypes.Message, OnMessageAsync);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles when new members are added to the conversation.
    /// Sends a welcome message to each new member (excluding the bot itself).
    /// </summary>
    protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            // Don't send welcome message to the bot itself
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Hello and Welcome!"), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Main message handler that processes user messages using Azure AI Foundry Agents.
    /// Creates/retrieves agent and thread, runs the agent with MCP tools, handles tool approvals,
    /// and streams the response back to the user.
    /// </summary>
    protected async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        try
        {
            #region Initial Setup and Logging

            // Send initial working message to user
            await turnContext.StreamingResponse.QueueInformativeUpdateAsync("Working on it...", cancellationToken);

            var userMessage = turnContext.Activity.Text;
            var conversationId = turnContext.Activity.Conversation.Id;

            _logger.LogInformation("Processing message from user in conversation {ConversationId}: {Message}",
                conversationId, userMessage);

            #endregion

            #region Agent Management

            // Get or create the agent - reuse existing agent ID from conversation state if available
            // This ensures we maintain the same agent across multiple messages in a conversation
            var agentId = turnState.Conversation.AgentId();
            if (string.IsNullOrEmpty(agentId))
            {
                agentId = await _azureAIAgentService.GetOrCreateAgentAsync(null);
                turnState.Conversation.AgentId(agentId);
                _logger.LogInformation("Created new Agent with ID: {AgentId} for conversation {ConversationId}", agentId, conversationId);
            }
            else
            {
                _logger.LogInformation("Reusing existing Agent with ID: {AgentId} for conversation {ConversationId}", agentId, conversationId);
            }

            #endregion

            #region Thread Management

            // Get or create a thread for this conversation
            // Threads maintain the conversation history for the agent
            string threadId = turnState.Conversation.SerializedThread();
            PersistentAgentThread persistentThread;
            
            if (string.IsNullOrEmpty(threadId))
            {
                // Create a new thread for this conversation
                var createResponse = await _client.Threads.CreateThreadAsync();
                persistentThread = createResponse.Value;
                threadId = persistentThread.Id;
                
                // Save the thread ID to conversation state for future messages
                turnState.Conversation.SerializedThread(threadId);
                
                _logger.LogInformation("Created new thread {ThreadId} for conversation {ConversationId}", threadId, conversationId);
            }
            else
            {
                // Retrieve existing thread to continue the conversation
                var getResponse = await _client.Threads.GetThreadAsync(threadId);
                persistentThread = getResponse.Value;
                
                _logger.LogInformation("Reusing existing thread {ThreadId} for conversation {ConversationId}", threadId, conversationId);
            }

            #endregion

            #region User Feedback

            // Send typing indicator to show the bot is processing
            await turnContext.SendActivitiesAsync([new Activity { Type = ActivityTypes.Typing }], cancellationToken);

            // Track and display message count for this conversation
            int count = turnState.Conversation.IncrementMessageCount();
            turnContext.StreamingResponse.QueueTextChunk($"({count}) ");

            #endregion

            #region Create Message and Run Agent

            _logger.LogInformation("Running AI Agent for conversation {ConversationId}", conversationId);

            // Add the user's message to the thread
            var messageResponse = await _client.Messages.CreateMessageAsync(
                threadId,
                MessageRole.User,
                userMessage);
            PersistentThreadMessage message = messageResponse.Value;
            
            _logger.LogInformation("Created message in thread {ThreadId}", threadId);

            // Get the persistent agent instance
            var agentResponse = await _client.Administration.GetAgentAsync(agentId);
            var persistentAgent = agentResponse.Value;

            // Get MCP tool resources with authentication headers for Elastic search
            ToolResources toolResources = _azureAIAgentService.CreateMcpToolResources();

            // Create and start a run - this executes the agent on the thread
            var runResponse = await _client.Runs.CreateRunAsync(
                persistentThread,
                persistentAgent,
                toolResources: toolResources);
            ThreadRun run = runResponse.Value;
            
            _logger.LogInformation("Started run {RunId} on thread {ThreadId}", run.Id, threadId);

            #endregion

            #region Poll for Completion and Handle Tool Approvals

            // Poll for run completion and handle any required tool approvals
            // The agent may need to call MCP tools (e.g., Elastic search) which require approval
            while (run.Status == RunStatus.Queued || 
                   run.Status == RunStatus.InProgress || 
                   run.Status == RunStatus.RequiresAction)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                run = await _client.Runs.GetRunAsync(threadId, run.Id, cancellationToken);
                
                _logger.LogDebug("Run status: {Status}", run.Status);
                
                // Log any errors that occurred during the run
                if (run.LastError != null)
                {
                    _logger.LogError("Run error: {ErrorMessage}", run.LastError.Message);
                }
                
                // Handle tool approval requests from the agent
                if (run.Status == RunStatus.RequiresAction && 
                    run.RequiredAction is SubmitToolApprovalAction toolApprovalAction)
                {
                    var toolApprovals = new List<ToolApproval>();
                    
                    // Approve each MCP tool call and add authorization headers
                    foreach (var toolCall in toolApprovalAction.SubmitToolApproval.ToolCalls)
                    {
                        if (toolCall is RequiredMcpToolCall mcpToolCall)
                        {
                            _logger.LogInformation("Approving MCP tool call: {ToolName}, Arguments: {Arguments}", 
                                mcpToolCall.Name, mcpToolCall.Arguments);
                            
                            // Create approval with Elastic API key in authorization header
                            var approval = new ToolApproval(mcpToolCall.Id, approve: true);
                            approval.Headers["Authorization"] = $"ApiKey {_azureAIAgentService.GetElasticApiKey()}";
                            toolApprovals.Add(approval);
                        }
                    }

                    // Submit all tool approvals back to the agent
                    if (toolApprovals.Count > 0)
                    {
                        _logger.LogInformation("Submitting {Count} tool approvals", toolApprovals.Count);
                        run = await _client.Runs.SubmitToolOutputsToRunAsync(
                            threadId, 
                            run.Id, 
                            toolApprovals: toolApprovals,
                            cancellationToken: cancellationToken);
                    }
                }
            }
            
            _logger.LogInformation("Run completed with status: {Status}", run.Status);

            #endregion

            #region Stream Response to User

            // Retrieve all messages from the thread
            var messagesPage = _client.Messages.GetMessagesAsync(
                threadId: threadId,
                order: ListSortOrder.Ascending,
                cancellationToken: cancellationToken);

            // Stream assistant messages (created after the user's message) back to the user
            await foreach (PersistentThreadMessage threadMessage in messagesPage)
            {
                if (threadMessage.Role == "assistant" && 
                    threadMessage.CreatedAt > message.CreatedAt)
                {
                    foreach (MessageContent contentItem in threadMessage.ContentItems)
                    {
                        if (contentItem is MessageTextContent textItem)
                        {
                            turnContext.StreamingResponse.QueueTextChunk(textItem.Text);
                        }
                        else if (contentItem is MessageImageFileContent imageFileItem)
                        {
                            turnContext.StreamingResponse.QueueTextChunk($"[Image: {imageFileItem.FileId}]");
                        }
                    }
                }
            }

            _logger.LogInformation("AI Agent completed streaming response for conversation {ConversationId}", conversationId);

            #endregion
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in RiskAgentBot for conversation {ConversationId}",
                turnContext.Activity.Conversation.Id);
            await turnContext.SendActivityAsync("Sorry, I encountered an error processing your message. Please try again later.", cancellationToken: cancellationToken);
        }
        finally
        {
            // Always indicate that processing is complete
            await turnContext.StreamingResponse.EndStreamAsync(cancellationToken);
        }
    }

    #endregion
}
