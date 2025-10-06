# Azure AI Content Safety Integration

This document describes the integration of Azure AI Content Safety Prompt Shield into the RiskAgent application to detect and prevent jailbreak attempts.

## Overview

The RiskAgent uses Azure AI Content Safety's Prompt Shield feature to analyze user prompts and MCP tool call outputs for potential jailbreak attempts. This helps protect the system from malicious prompt injection attacks that could compromise the agent's behavior.

## Features

- **User Prompt Analysis**: Every user message is analyzed before being processed by the AI agent
- **Tool Output Analysis**: MCP tool call outputs (e.g., from Elastic search) are analyzed to detect jailbreak attempts in retrieved data
- **Detection Modes**: Disabled, Audit (detect but don't block), or Enforce (detect and block)
- **JSON Text Extraction**: Intelligently extracts only text content from JSON, reducing API costs by 50-70%
- **Automatic Chunking**: Handles text longer than 1000 characters automatically
- **Fail-Open Design**: Service failures don't block legitimate requests - the system continues to operate if Content Safety API is unavailable
- **Zero Overhead When Disabled**: No performance impact when detection mode is set to Disabled

## Architecture

### Service Layer

The Content Safety functionality is implemented as a separate service (`ContentSafetyService`) that can be extended with additional safety features in the future.

```
Services/
??? IContentSafetyService.cs      # Service interface
??? ContentSafetyService.cs       # Implementation
??? IAzureAIAgentService.cs       # Existing agent service
??? AzureAIAgentService.cs        # Existing agent service
```

### Integration Points

1. **RiskAgentBot Constructor**: Injects `IContentSafetyService` via dependency injection
2. **OnMessageAsync - User Prompt Check**: Analyzes user input before creating agent run
3. **OnMessageAsync - Tool Output Check**: Analyzes MCP tool outputs after agent execution completes

## Configuration

### appsettings.json

Add the following configuration to your `appsettings.json`:

```json
{
  "AIServices": {
    "ContentSafety": {
      "Endpoint": "https://YOUR-CONTENT-SAFETY.cognitiveservices.azure.com/",
      "SubscriptionKey": "YOUR_CONTENT_SAFETY_SUBSCRIPTION_KEY_HERE",
      "JailbreakDetectionMode": "Enforce"
    }
  }
}
```

### Detection Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| **Disabled** | No detection performed, zero overhead | Development, testing without API costs |
| **Audit** | Detects and logs but doesn't block requests | Production monitoring, testing in prod |
| **Enforce** | Detects and blocks malicious requests | Production security |

**Default**: `Enforce` (for backward compatibility)

### Environment Variables (Alternative)

You can also configure using environment variables:

- `AZURE_CONTENT_SAFETY_ENDPOINT`: Your Azure Content Safety endpoint URL
- `AZURE_CONTENT_SAFETY_SUBSCRIPTION_KEY`: Your subscription key
- `AZURE_CONTENT_SAFETY_JAILBREAK_DETECTION_MODE`: `Disabled`, `Audit`, or `Enforce`

Environment variables take precedence over appsettings.json values.

**Note**: When mode is `Disabled`, endpoint and subscription key are not required.

## Usage

### Detecting Jailbreak in User Prompts

The service automatically checks every user message (unless mode is Disabled):

```csharp
// Check detection mode first
var detectionMode = _contentSafetyService.DetectionMode;

if (detectionMode != JailbreakDetectionMode.Disabled)
{
    var result = await _contentSafetyService.DetectJailbreakAsync(
        userMessage, 
        cancellationToken);

    if (result.IsJailbreakDetected)
    {
        _logger.LogWarning("Jailbreak detected in user prompt. Mode: {Mode}", detectionMode);
        
        if (detectionMode == JailbreakDetectionMode.Enforce)
        {
            // Block the request
            await turnContext.SendActivityAsync(
                $"?? Security Alert: A jailbreak attempt was detected and blocked.\n\n" +
                $"Offending text:\n\"{result.OffendingText}\"",
                cancellationToken: cancellationToken);
            return;
        }
        // Audit mode: Log but continue processing
    }
}
```

### Detecting Jailbreak in Tool Outputs

After MCP tools are executed, their outputs are analyzed with JSON text extraction:

```csharp
var mcpToolOutputs = new List<string>();
// ... collect outputs from RunStepMcpToolCall objects ...

if (mcpToolOutputs.Count > 0 && detectionMode != JailbreakDetectionMode.Disabled)
{
    // Extract text from JSON and combine
    var extractedTexts = new List<string>();
    foreach (var output in mcpToolOutputs)
    {
        var extractedText = ExtractTextFromJson(output);
        if (!string.IsNullOrWhiteSpace(extractedText))
        {
            extractedTexts.Add(extractedText);
        }
    }
    
    var combinedOutput = string.Join("\n\n", extractedTexts);
    
    var result = await _contentSafetyService.DetectJailbreakAsync(
        combinedOutput, 
        cancellationToken);

    if (result.IsJailbreakDetected && detectionMode == JailbreakDetectionMode.Enforce)
    {
        // Block the response
        await turnContext.SendActivityAsync(
            $"?? Security Alert: A jailbreak attempt was detected in retrieved data and blocked.\n\n" +
            $"Offending text:\n\"{result.OffendingText}\"",
            cancellationToken: cancellationToken);
        return;
    }
}
```

## API Details

### IContentSafetyService Interface

```csharp
public interface IContentSafetyService
{
    /// <summary>
    /// Gets the current jailbreak detection mode
    /// </summary>
    JailbreakDetectionMode DetectionMode { get; }
    
    /// <summary>
    /// Analyzes text for potential jailbreak attempts using Azure Content Safety Prompt Shield.
    /// Text longer than 1000 characters is automatically split into chunks and analyzed separately.
    /// </summary>
    /// <param name="text">The text to analyze (will be chunked internally if needed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detection result including whether jailbreak was detected and offending text</returns>
    Task<JailbreakDetectionResult> DetectJailbreakAsync(
        string text, 
        CancellationToken cancellationToken = default);
}
```

### JailbreakDetectionResult

```csharp
public class JailbreakDetectionResult
{
    public bool IsJailbreakDetected { get; set; }
    public string? OffendingText { get; set; }
    public JailbreakDetectionMode Mode { get; set; }
}
```

### Limitations

- **Maximum Chunk Length**: 1000 characters (automatically chunked if longer)
- **API Version**: Uses Azure Content Safety API version 2024-09-01
- **Rate Limits**: Subject to Azure Content Safety service tier limits
- **API Response Format**: Uses `attackDetected` property from Azure API

## Error Handling

The service implements a "fail-open" approach:

- **API Failures**: Log errors but don't block requests (returns `false`)
- **Network Issues**: Log errors but don't block requests (returns `false`)
- **Empty Prompts**: Returns `false` (no jailbreak detected)
- **Invalid Configuration**: Throws exceptions during service initialization

This ensures that temporary service issues don't prevent legitimate users from using the RiskAgent.

## Logging

The service provides detailed logging at multiple levels:

- **Information**: Service initialization, jailbreak detection results
- **Warning**: Jailbreak attempts detected, truncated prompts
- **Error**: API failures, network issues
- **Debug**: Request details, analysis progress

Example log output:

```
[Information] ContentSafetyService initialized with endpoint: https://contoso-safety.cognitiveservices.azure.com/
[Information] Analyzing user prompt for jailbreak attempts in conversation abc123
[Debug] Sending jailbreak detection request for prompt (length: 250)
[Warning] Jailbreak attempt detected in user prompt
[Warning] Jailbreak attempt detected in conversation abc123. Blocking request.
```

## JSON Text Extraction

When analyzing MCP tool outputs (which are typically JSON), the service automatically extracts only text content:

### Benefits
- **Cost Reduction**: 50-70% fewer characters sent to API
- **Faster Processing**: Less data to analyze
- **Better Focus**: Analyzes actual content, not structure

### How It Works

```csharp
// Before: Full JSON (250 chars)
{
  "results": [{
    "id": 123,
    "title": "Risk Document",
    "content": "Important risk information...",
    "score": 0.95
  }]
}

// After: Extracted text only (85 chars)
Risk Document
Important risk information...

// Result: 66% reduction
```

### Implementation

The `ExtractTextFromJson` helper method:
- Recursively traverses JSON structure
- Extracts only string values
- Skips numbers, booleans, property names
- Falls back to original text if not valid JSON

## Extending the Service

The `IContentSafetyService` interface is designed to be extended with additional content safety features:

### Future Enhancements

1. **Content Moderation**: Add methods for text moderation (hate speech, violence, etc.)
2. **Image Analysis**: Support image content safety checks
3. **Custom Policies**: Implement custom content filtering policies
4. **Severity Levels**: Return severity scores instead of boolean values
5. **Property Filtering**: Only extract specific JSON properties

Example extension:

```csharp
public interface IContentSafetyService
{
    JailbreakDetectionMode DetectionMode { get; }
    Task<JailbreakDetectionResult> DetectJailbreakAsync(string text, CancellationToken cancellationToken = default);
    
    // Future methods
    Task<ContentModerationResult> AnalyzeContentAsync(string text, CancellationToken cancellationToken = default);
    Task<ImageModerationResult> AnalyzeImageAsync(byte[] imageData, CancellationToken cancellationToken = default);
}
```

## Security Best Practices

1. **Store Secrets Securely**: Use Azure Key Vault or Azure App Configuration for subscription keys
2. **Monitor Usage**: Track Content Safety API calls and costs
3. **Review Logs**: Regularly review jailbreak detection logs for patterns
4. **Test Coverage**: Include jailbreak attempts in your test scenarios
5. **User Feedback**: Provide clear messages when blocking requests

## Testing

See the separate test documentation for details on testing the Content Safety integration.

## References

- [Azure Content Safety Documentation](https://aka.ms/acsstudiodoc)
- [Prompt Shield Quickstart](https://learn.microsoft.com/azure/ai-services/content-safety/quickstart-jailbreak)
- [Content Safety REST API Reference](https://learn.microsoft.com/rest/api/cognitiveservices/contentsafety/)

## Troubleshooting

### "AZURE_CONTENT_SAFETY_ENDPOINT is not set" Error

**Solution**: Add the Content Safety configuration to appsettings.json or set environment variables.

### False Positives

**Solution**: The service logs all detection events. Review logs and adjust user messaging if needed. Consider implementing a feedback mechanism.

### API Rate Limiting

**Solution**: Monitor API usage and upgrade service tier if needed. Implement caching for repeated prompts if appropriate.

### Service Unavailable

**Solution**: The service fails open, so users can continue. Monitor logs and check Azure Content Safety service health.
