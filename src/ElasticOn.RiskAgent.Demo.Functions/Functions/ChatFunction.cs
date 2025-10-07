using System.Net;
using System.Text.Json;
using Azure.AI.Agents.Persistent;
using ElasticOn.RiskAgent.Demo.Functions.Models;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using Markdig;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ElasticOn.RiskAgent.Demo.Functions.Functions;

/// <summary>
/// HTTP-triggered function that processes chat messages using Azure AI Foundry Agents with Elastic MCP tools.
/// Similar to RiskAgentBot but as a stateless HTTP API for the Blazor WebAssembly frontend.
/// </summary>
public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    private readonly IAzureAIAgentService _azureAIAgentService;
    private readonly IContentSafetyService _contentSafetyService;
    private readonly IChatStateService _chatStateService;
    private readonly PersistentAgentsClient _client;

    public ChatFunction(
        ILogger<ChatFunction> logger,
        IAzureAIAgentService azureAIAgentService,
        IContentSafetyService contentSafetyService,
        IChatStateService chatStateService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _azureAIAgentService = azureAIAgentService ?? throw new ArgumentNullException(nameof(azureAIAgentService));
        _contentSafetyService = contentSafetyService ?? throw new ArgumentNullException(nameof(contentSafetyService));
        _chatStateService = chatStateService ?? throw new ArgumentNullException(nameof(chatStateService));
        
        _client = _azureAIAgentService.GetClient();
    }

    [Function("Chat")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse the request
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, "Request body is empty", HttpStatusCode.BadRequest);
            }

            // Configure JSON options to handle camelCase from JavaScript/Blazor
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var request = JsonSerializer.Deserialize<SendMessageRequest>(requestBody, jsonOptions);
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return await CreateErrorResponse(req, "Invalid request: Message is required", HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrWhiteSpace(request.ConversationId))
            {
                return await CreateErrorResponse(req, "Invalid request: ConversationId is required", HttpStatusCode.BadRequest);
            }

            var userMessage = request.Message;
            var conversationId = request.ConversationId;

            _logger.LogInformation("Processing message from conversation {ConversationId}: {Message}",
                conversationId, userMessage);

            #region Content Safety - Jailbreak Detection on User Prompt

            // Determine detection mode: use request override if provided, otherwise use configuration
            var detectionMode = _contentSafetyService.DetectionMode;
            if (!string.IsNullOrWhiteSpace(request.ContentSafetyMode))
            {
                if (Enum.TryParse<JailbreakDetectionMode>(request.ContentSafetyMode, true, out var requestedMode))
                {
                    detectionMode = requestedMode;
                    _logger.LogInformation("Using Content Safety mode from request: {Mode} (overriding configuration)", detectionMode);
                }
                else
                {
                    _logger.LogWarning("Invalid ContentSafetyMode '{Mode}' in request, using configuration default: {DefaultMode}", 
                        request.ContentSafetyMode, detectionMode);
                }
            }
            else
            {
                _logger.LogInformation("Using Content Safety mode from configuration: {Mode}", detectionMode);
            }
            
            var detectionResult = new JailbreakDetectionResult { IsJailbreakDetected = false };

            if (detectionMode != JailbreakDetectionMode.Disabled)
            {
                _logger.LogInformation("Running jailbreak detection on user prompt in {Mode} mode", detectionMode);
                detectionResult = await _contentSafetyService.DetectJailbreakAsync(userMessage, cancellationToken);

                if (detectionResult.IsJailbreakDetected)
                {
                    _logger.LogWarning("Jailbreak attempt detected in user prompt for conversation {ConversationId}", conversationId);

                    if (detectionMode == JailbreakDetectionMode.Enforce)
                    {
                        // Enforce mode: Return security alert message instead of blocking
                        _logger.LogWarning("Returning security alert due to user prompt jailbreak detection in Enforce mode");
                        var securityAlertMessage = "⚠️ Your message has been flagged by our content safety system. Please rephrase your question.";
                        
                        // Convert markdown to HTML
                        var userPromptPipeline = new MarkdownPipelineBuilder()
                            .UseAdvancedExtensions()
                            .Build();
                        var securityAlertHtml = Markdown.ToHtml(securityAlertMessage, userPromptPipeline);
                        
                        var userPromptResponse = req.CreateResponse(HttpStatusCode.OK);
                        userPromptResponse.Headers.Add("Content-Type", "application/json");
                        
                        var userPromptResponseData = new SendMessageResponse
                        {
                            Success = true,
                            Message = securityAlertMessage,
                            MessageHtml = securityAlertHtml,
                            ThreadId = request.ThreadId // Use request ThreadId since we haven't created one yet
                        };
                        
                        var userPromptJson = JsonSerializer.Serialize(userPromptResponseData, GetJsonOptions());
                        await userPromptResponse.WriteStringAsync(userPromptJson, cancellationToken);
                        return userPromptResponse;
                    }
                    
                    _logger.LogWarning("Jailbreak detected but continuing in Audit mode");
                }
                else
                {
                    _logger.LogInformation("No jailbreak detected in user prompt");
                }
            }
            else
            {
                _logger.LogInformation("Jailbreak detection is disabled");
            }

            #endregion

            #region Agent Management

            // Get or create agent
            var agentId = _chatStateService.GetAgentId(conversationId);
            if (string.IsNullOrEmpty(agentId))
            {
                agentId = await _azureAIAgentService.GetOrCreateAgentAsync();
                _chatStateService.SetAgentId(conversationId, agentId);
                _logger.LogInformation("Created new agent {AgentId} for conversation {ConversationId}", agentId, conversationId);
            }
            else
            {
                _logger.LogInformation("Using existing agent {AgentId} for conversation {ConversationId}", agentId, conversationId);
            }

            #endregion

            #region Thread Management

            // Get or create thread
            var threadId = request.ThreadId ?? _chatStateService.GetThreadId(conversationId);
            PersistentAgentThread persistentThread;
            
            if (string.IsNullOrEmpty(threadId))
            {
                var threadResponse = await _client.Threads.CreateThreadAsync();
                persistentThread = threadResponse.Value;
                threadId = persistentThread.Id;
                _chatStateService.SetThreadId(conversationId, threadId);
                _logger.LogInformation("Created new thread {ThreadId} for conversation {ConversationId}", threadId, conversationId);
            }
            else
            {
                var threadResponse = await _client.Threads.GetThreadAsync(threadId);
                persistentThread = threadResponse.Value;
                _logger.LogInformation("Using existing thread {ThreadId} for conversation {ConversationId}", threadId, conversationId);
            }

            #endregion

            #region Create Message and Run Agent

            // Add user message to thread
            var messageResponse = await _client.Messages.CreateMessageAsync(
                threadId,
                MessageRole.User,
                userMessage);
            var message = messageResponse.Value;

            _logger.LogInformation("Added user message to thread {ThreadId}", threadId);

            // Get the persistent agent instance
            var agentResponse = await _client.Administration.GetAgentAsync(agentId);
            var persistentAgent = agentResponse.Value;

            // Create a run with MCP tool resources
            var toolResources = _azureAIAgentService.CreateMcpToolResources();
            var runResponse = await _client.Runs.CreateRunAsync(
                persistentThread,
                persistentAgent,
                toolResources: toolResources);

            var run = runResponse.Value;
            _logger.LogInformation("Created run {RunId} for thread {ThreadId}", run.Id, threadId);

            #endregion

            #region Poll for Completion and Handle Tool Approvals

            // Poll until completion
            while (run.Status == RunStatus.Queued ||
                   run.Status == RunStatus.InProgress ||
                   run.Status == RunStatus.RequiresAction)
            {
                await Task.Delay(500, cancellationToken);
                run = await _client.Runs.GetRunAsync(threadId, run.Id, cancellationToken);

                if (run.LastError != null)
                {
                    _logger.LogError("Run error: {Code} - {Message}", run.LastError.Code, run.LastError.Message);
                }

                _logger.LogDebug("Run status: {Status}", run.Status);
            }

            #endregion

            #region Check for Run Failures

            // Check for run failures
            if (run.Status == RunStatus.Failed)
            {
                var errorMessage = "The agent run failed.";
                
                if (run.LastError != null)
                {
                    errorMessage = $"Error: {run.LastError.Message}";
                    _logger.LogError("Run failed: {Code} - {Message}", run.LastError.Code, run.LastError.Message);
                }
                else
                {
                    _logger.LogError("Run failed with no error details");
                }
                
                return await CreateErrorResponse(req, errorMessage, HttpStatusCode.InternalServerError);
            }
            else if (run.Status == RunStatus.Cancelled)
            {
                _logger.LogWarning("Run was cancelled for thread {ThreadId}", threadId);
                return await CreateErrorResponse(req, "The request was cancelled.", HttpStatusCode.RequestTimeout);
            }
            else if (run.Status == RunStatus.Expired)
            {
                _logger.LogWarning("Run expired for thread {ThreadId}", threadId);
                return await CreateErrorResponse(req, "The request expired.", HttpStatusCode.RequestTimeout);
            }
            else if (run.Status != RunStatus.Completed)
            {
                _logger.LogError("Unexpected run status: {Status}", run.Status);
                return await CreateErrorResponse(req, "An unexpected error occurred.", HttpStatusCode.InternalServerError);
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
                            
                            var outputPreview = mcpToolCall.Output ?? "(null)";
                            if (outputPreview.Length > 100)
                            {
                                outputPreview = outputPreview.Substring(0, 100) + "...";
                            }
                            _logger.LogInformation("  Tool Output: {Output}", outputPreview);

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
            JailbreakDetectionResult? toolOutputDetectionResult = null;
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
                        // Enforce mode: Return security alert message instead of blocking
                        _logger.LogWarning("Returning security alert due to Enforce mode");
                        var securityAlertMessage = $"⚠️ Security Alert: A jailbreak attempt was detected in retrieved data and blocked.\n\nOffending text:\n\"{toolOutputDetectionResult.OffendingText}\"";
                        
                        // Convert markdown to HTML
                        var toolOutputPipeline = new MarkdownPipelineBuilder()
                            .UseAdvancedExtensions()
                            .Build();
                        var securityAlertHtml = Markdown.ToHtml(securityAlertMessage, toolOutputPipeline);
                        
                        var toolOutputResponse = req.CreateResponse(HttpStatusCode.OK);
                        toolOutputResponse.Headers.Add("Content-Type", "application/json");
                        
                        var toolOutputResponseData = new SendMessageResponse
                        {
                            Success = true,
                            Message = securityAlertMessage,
                            MessageHtml = securityAlertHtml,
                            ThreadId = threadId
                        };
                        
                        var toolOutputJson = JsonSerializer.Serialize(toolOutputResponseData, GetJsonOptions());
                        await toolOutputResponse.WriteStringAsync(toolOutputJson, cancellationToken);
                        return toolOutputResponse;
                    }
                    // Audit mode: Continue processing but will note the detection in the response
                }
                else
                {
                    _logger.LogDebug("No jailbreak detected in MCP tool outputs for conversation {ConversationId}", conversationId);
                }
            }

            #endregion

            #region Get Response Messages

            // Get the assistant's response from the thread
            var responseTextBuilder = new System.Text.StringBuilder();
            var messagesPage = _client.Messages.GetMessagesAsync(
                threadId: threadId,
                order: ListSortOrder.Ascending);

            // Collect assistant messages created after the user's message
            await foreach (PersistentThreadMessage threadMessage in messagesPage)
            {
                if (threadMessage.Role == "assistant" &&
                    threadMessage.CreatedAt > message.CreatedAt)
                {
                    foreach (MessageContent contentItem in threadMessage.ContentItems)
                    {
                        if (contentItem is MessageTextContent textItem)
                        {
                            responseTextBuilder.Append(textItem.Text);
                        }
                    }
                }
            }

            var responseText = responseTextBuilder.ToString();
            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogError("No assistant message found in thread {ThreadId}", threadId);
                return await CreateErrorResponse(req, "No response from agent.", HttpStatusCode.InternalServerError);
            }

            _logger.LogInformation("Successfully processed message for conversation {ConversationId}", conversationId);

            #endregion

            #region Build Response

            // Include audit information if in audit mode
            var finalResponse = responseText;
            if (detectionMode == JailbreakDetectionMode.Audit)
            {
                if (detectionResult.IsJailbreakDetected)
                {
                    finalResponse = $"⚠️ [AUDIT] Jailbreak detected in user prompt\n\n{responseText}";
                }
                
                if (toolOutputDetectionResult?.IsJailbreakDetected == true)
                {
                    finalResponse = $"⚠️ [AUDIT] Jailbreak detected in tool outputs\n\n{finalResponse}";
                }
            }

            // Convert markdown to HTML using Markdig with advanced extensions
            // UseAdvancedExtensions includes: AutoLinks, Tables, TaskLists, Emphasis extras,
            // Lists, Footnotes, Citations, Math, GridTables, Abbreviations, and more
            var responsePipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
            var messageHtml = Markdown.ToHtml(finalResponse, responsePipeline);

            var finalHttpResponse = req.CreateResponse(HttpStatusCode.OK);
            finalHttpResponse.Headers.Add("Content-Type", "application/json");
            
            var finalResponseData = new SendMessageResponse
            {
                Success = true,
                Message = finalResponse,
                MessageHtml = messageHtml,
                ThreadId = threadId
            };
            
            var finalJson = JsonSerializer.Serialize(finalResponseData, GetJsonOptions());
            await finalHttpResponse.WriteStringAsync(finalJson, cancellationToken);

            return finalHttpResponse;

            #endregion
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return await CreateErrorResponse(req, $"An error occurred: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    #region Helper Methods

    /// <summary>
    /// Creates JSON serializer options for camelCase responses
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string errorMessage, HttpStatusCode statusCode)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        
        var responseData = new SendMessageResponse
        {
            Success = false,
            Error = errorMessage
        };
        
        var json = JsonSerializer.Serialize(responseData, GetJsonOptions());
        await response.WriteStringAsync(json);
        return response;
    }

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
            var doc = JsonDocument.Parse(json);
            var builder = new System.Text.StringBuilder();
            ExtractTextFromJsonElement(doc.RootElement, builder);
            return builder.ToString().Trim();
        }
        catch
        {
            // If JSON parsing fails, return the original string
            return json;
        }
    }

    /// <summary>
    /// Recursively extracts text from JSON elements
    /// </summary>
    private static void ExtractTextFromJsonElement(JsonElement element, System.Text.StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    ExtractTextFromJsonElement(property.Value, builder);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractTextFromJsonElement(item, builder);
                }
                break;

            case JsonValueKind.String:
                var stringValue = element.GetString();
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    builder.AppendLine(stringValue);
                }
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                // Skip numbers and booleans - we only care about text content
                break;
        }
    }

    #endregion
}
