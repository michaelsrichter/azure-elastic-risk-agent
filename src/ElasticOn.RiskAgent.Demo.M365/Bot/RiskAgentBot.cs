using Azure.AI.Agents.Persistent;
using ElasticOn.RiskAgent.Demo.M365.Services;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;

namespace ElasticOn.RiskAgent.Demo.M365.Bot;

/// <summary>
/// Bot implementation that uses Azure AI Foundry Agents with Elastic MCP tools to answer risk-related questions.
/// Maintains conversation state and handles message streaming back to users.
/// </summary>
public class RiskAgentBot : AgentApplication
{
    #region Fields

    private readonly IAzureAIAgentService _azureAIAgentService;
    private readonly IContentSafetyService _contentSafetyService;
    private readonly ILogger<RiskAgentBot> _logger;
    private readonly PersistentAgentsClient _client;

    #endregion

    #region Constructor

    public RiskAgentBot(
        AgentApplicationOptions options,
        IAzureAIAgentService azureAIAgentService,
        IContentSafetyService contentSafetyService,
        ILogger<RiskAgentBot> logger) : base(options)
    {
        _azureAIAgentService = azureAIAgentService ?? throw new ArgumentNullException(nameof(azureAIAgentService));
        _contentSafetyService = contentSafetyService ?? throw new ArgumentNullException(nameof(contentSafetyService));
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

            #region Content Safety - Jailbreak Detection on User Prompt

            // Check detection mode - skip analysis entirely if disabled
            var detectionMode = _contentSafetyService.DetectionMode;
            JailbreakDetectionResult detectionResult = null;

            if (detectionMode != JailbreakDetectionMode.Disabled)
            {
                _logger.LogInformation("Analyzing user prompt for jailbreak attempts in conversation {ConversationId} (Mode: {Mode})", 
                    conversationId, detectionMode);

                detectionResult = await _contentSafetyService.DetectJailbreakAsync(userMessage, cancellationToken);

                if (detectionResult.IsJailbreakDetected)
                {
                    _logger.LogWarning("Jailbreak attempt detected in conversation {ConversationId}. Mode: {Mode}", 
                        conversationId, detectionMode);

                    if (detectionMode == JailbreakDetectionMode.Enforce)
                    {
                        // Enforce mode: Block the request and show offending text
                        _logger.LogWarning("Blocking request due to Enforce mode");
                        await turnContext.SendActivityAsync(
                            $"⚠️ Security Alert: A jailbreak attempt was detected and blocked.\n\n" +
                            $"Offending text:\n\"{detectionResult.OffendingText}\"",
                            cancellationToken: cancellationToken);
                        return; // Block the request
                    }
                    // Audit mode: Continue processing but will note the detection in the response
                }
                else
                {
                    _logger.LogDebug("No jailbreak detected in user prompt for conversation {ConversationId}", conversationId);
                }
            }
            else
            {
                _logger.LogDebug("Jailbreak detection is disabled for conversation {ConversationId}", conversationId);
            }

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
                if (run.LastError != null)
                {
                    _logger.LogError("Failure Code: {Code}", run.LastError.Code);
                    _logger.LogError("Failure Message: {Message}", run.LastError.Message);
                }
                _logger.LogDebug("Run status: {Status}", run.Status);

                // Handle tool approval requests from the agent


            }

            #endregion

            #region Content Safety - Analyze MCP Tool Call Outputs

            // Collect MCP tool call outputs for jailbreak analysis
            var mcpToolOutputs = new List<string>();
            
            var runSteps = _client.Runs.GetRunStepsAsync(run);
            await foreach (var step in runSteps)
            {
                // Check if this is a tool call step
                if (step.StepDetails is RunStepToolCallDetails toolCallDetails)
                {
                    _logger.LogInformation("Tool Call Step - Tool Calls Count: {Count}", toolCallDetails.ToolCalls.Count);

                    foreach (var toolCall in toolCallDetails.ToolCalls)
                    {
                        if (toolCall is RunStepMcpToolCall mcpToolCall)
                        {
                            _logger.LogInformation("MCP Tool Call in Step:");
                            _logger.LogInformation("  Tool ID: {ToolId}", mcpToolCall.Id);
                            _logger.LogInformation("  Tool Name: {ToolName}", mcpToolCall.Name);
                            _logger.LogInformation("  Tool Arguments: {Arguments}", mcpToolCall.Arguments);
                            _logger.LogInformation("  Tool Output: {Output}", mcpToolCall.Output ?? "(null)");

                            // Collect outputs for content safety analysis
                            if (!string.IsNullOrWhiteSpace(mcpToolCall.Output))
                            {
                                mcpToolOutputs.Add(mcpToolCall.Output);
                            }
                        }
                    }
                }
            }

            // Analyze MCP tool outputs if any were collected (and detection is not disabled)
            JailbreakDetectionResult toolOutputDetectionResult = null;
            if (mcpToolOutputs.Count > 0 && detectionMode != JailbreakDetectionMode.Disabled)
            {
                // Extract text content from JSON outputs to save characters and focus on actual content
                var extractedTexts = new List<string>();
                int originalTotalLength = 0;
                
                foreach (var output in mcpToolOutputs)
                {
                    originalTotalLength += output.Length;
                    var extractedText = ExtractTextFromJson(output);
                    if (!string.IsNullOrWhiteSpace(extractedText))
                    {
                        extractedTexts.Add(extractedText);
                    }
                }
                
                // Combine all extracted text into a single document for analysis
                var combinedOutput = string.Join("\n\n", extractedTexts);
                
                _logger.LogInformation("Analyzing MCP tool outputs (extracted {ExtractedLength} chars from {OriginalLength} original chars, {Count} tool calls) for jailbreak attempts in conversation {ConversationId} (Mode: {Mode})", 
                    combinedOutput.Length, originalTotalLength, mcpToolOutputs.Count, conversationId, detectionMode);

                toolOutputDetectionResult = await _contentSafetyService.DetectJailbreakAsync(
                    combinedOutput, 
                    cancellationToken);

                if (toolOutputDetectionResult.IsJailbreakDetected)
                {
                    _logger.LogWarning("Jailbreak attempt detected in MCP tool outputs for conversation {ConversationId}. Mode: {Mode}", 
                        conversationId, detectionMode);

                    if (detectionMode == JailbreakDetectionMode.Enforce)
                    {
                        // Enforce mode: Block the response and show offending text
                        _logger.LogWarning("Blocking response due to Enforce mode");
                        await turnContext.SendActivityAsync(
                            $"⚠️ Security Alert: A jailbreak attempt was detected in retrieved data and blocked.\n\n" +
                            $"Offending text:\n\"{toolOutputDetectionResult.OffendingText}\"",
                            cancellationToken: cancellationToken);
                        return; // Block the response
                    }
                    // Audit mode: Continue processing but will note the detection in the response
                }
                else
                {
                    _logger.LogDebug("No jailbreak detected in MCP tool outputs for conversation {ConversationId}", conversationId);
                }
            }


            _logger.LogInformation("Run completed with status: {Status}", run.Status);

            // Handle non-completed run statuses and notify the user
            if (run.Status == RunStatus.Failed)
            {
                _logger.LogError("Run {RunId} FAILED for conversation {ConversationId}", run.Id, conversationId);
                
                if (run.LastError != null)
                {
                    _logger.LogError("Failure Code: {Code}", run.LastError.Code);
                    _logger.LogError("Failure Message: {Message}", run.LastError.Message);
                }
                else
                {
                    _logger.LogError("Run failed but no error details available");
                }

                // Check if there are failed steps
                var failedSteps = _client.Runs.GetRunStepsAsync(run);
                await foreach (var step in failedSteps)
                {
                    if (step.Status == RunStepStatus.Failed)
                    {
                        _logger.LogError("Failed Step ID: {StepId}, Type: {StepType}", step.Id, step.Type);
                        
                        if (step.LastError != null)
                        {
                            _logger.LogError("Step Error Code: {Code}", step.LastError.Code);
                            _logger.LogError("Step Error Message: {Message}", step.LastError.Message);
                        }
                    }
                }

                // Notify user about the failure
                await turnContext.SendActivityAsync(
                    "I encountered an error while processing your request. Please try again.", 
                    cancellationToken: cancellationToken);
                return; // Exit early, don't try to stream response
            }
            else if (run.Status == RunStatus.Cancelled)
            {
                _logger.LogWarning("Run {RunId} was CANCELLED for conversation {ConversationId}", run.Id, conversationId);
                
                await turnContext.SendActivityAsync(
                    "Your request was cancelled. Please try again if you'd like to continue.", 
                    cancellationToken: cancellationToken);
                return; // Exit early
            }
            else if (run.Status == RunStatus.Expired)
            {
                _logger.LogWarning("Run {RunId} EXPIRED for conversation {ConversationId}", run.Id, conversationId);
                
                await turnContext.SendActivityAsync(
                    "Your request took too long to process and has expired. Please try again with a simpler query.", 
                    cancellationToken: cancellationToken);
                return; // Exit early
            }
            else if (run.Status != RunStatus.Completed)
            {
                // Catch any other unexpected non-completed status
                _logger.LogWarning("Run {RunId} ended with unexpected status {Status} for conversation {ConversationId}", 
                    run.Id, run.Status, conversationId);
                
                await turnContext.SendActivityAsync(
                    "Something unexpected happened while processing your request. Please try again.", 
                    cancellationToken: cancellationToken);
                return; // Exit early
            }

            #endregion

            #region Stream Response to User

            // In Audit mode, prepend security notes if jailbreak was detected
            if (detectionMode == JailbreakDetectionMode.Audit)
            {
                if (detectionResult.IsJailbreakDetected)
                {
                    turnContext.StreamingResponse.QueueTextChunk(
                        "📋 **Audit Note**: Jailbreak attempt detected in user prompt. " +
                        "Processing continued for audit purposes.\n\n");
                }
                
                if (toolOutputDetectionResult?.IsJailbreakDetected == true)
                {
                    turnContext.StreamingResponse.QueueTextChunk(
                        "📋 **Audit Note**: Jailbreak attempt detected in retrieved data. " +
                        "Processing continued for audit purposes.\n\n");
                }
            }

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

    #region Helper Methods

    /// <summary>
    /// Extracts text content from JSON by removing JSON structure and keeping only string values.
    /// This reduces the amount of data sent to Content Safety API and focuses on actual content.
    /// </summary>
    /// <param name="json">JSON string to extract text from</param>
    /// <returns>Extracted text content</returns>
    private static string ExtractTextFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            // Try to parse as JSON
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var textBuilder = new System.Text.StringBuilder();
            ExtractTextFromJsonElement(document.RootElement, textBuilder);
            return textBuilder.ToString().Trim();
        }
        catch
        {
            // If it's not valid JSON, return the original text
            return json;
        }
    }

    /// <summary>
    /// Recursively extracts text from JSON elements
    /// </summary>
    private static void ExtractTextFromJsonElement(System.Text.Json.JsonElement element, System.Text.StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    ExtractTextFromJsonElement(property.Value, builder);
                }
                break;

            case System.Text.Json.JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractTextFromJsonElement(item, builder);
                }
                break;

            case System.Text.Json.JsonValueKind.String:
                var stringValue = element.GetString();
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    builder.AppendLine(stringValue);
                }
                break;

            case System.Text.Json.JsonValueKind.Number:
            case System.Text.Json.JsonValueKind.True:
            case System.Text.Json.JsonValueKind.False:
                // Skip numbers and booleans - we only care about text content
                break;
        }
    }

    #endregion
}
