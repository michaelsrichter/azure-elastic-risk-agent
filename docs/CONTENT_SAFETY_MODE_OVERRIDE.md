# Content Safety Mode Override Feature

## Summary

Added the ability to override the Content Safety detection mode from the web application UI, allowing users to dynamically choose between Disabled, Audit, and Enforce modes without changing the server configuration.

## Changes Made

### 1. Model Updates

#### Web Model (`/src/ElasticOn.RiskAgent.Demo.Web/Models/ChatModels.cs`)
- Added `ContentSafetyMode` property to `SendMessageRequest` class
- Property is optional (nullable string) with valid values: "Disabled", "Audit", "Enforce"

#### Functions Model (`/src/ElasticOn.RiskAgent.Demo.Functions/Models/SendMessageRequest.cs`)
- Added matching `ContentSafetyMode` property to `SendMessageRequest` class
- Maintains consistency between client and server models

### 2. Backend Logic (`/src/ElasticOn.RiskAgent.Demo.Functions/Functions/ChatFunction.cs`)

Updated the Content Safety detection logic to:
1. Check if `ContentSafetyMode` is provided in the request
2. If provided and valid, use it as an override
3. If not provided or invalid, fall back to the configuration value
4. Log which mode is being used for debugging

**Key Code Addition:**
```csharp
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
```

### 3. UI Updates (`/src/ElasticOn.RiskAgent.Demo.Web/Components/ChatComponent.razor`)

**Added Content Safety Selector:**
- Dropdown/select element above the chat input
- Three options: Disabled (default), Audit, Enforce
- Disabled during message processing
- Persists across messages in the same session

**Code Changes:**
- Added `selectedContentSafetyMode` variable (default: "Disabled")
- Updated `SendMessage` method to include `ContentSafetyMode` in API request

### 4. Styling (`/src/ElasticOn.RiskAgent.Demo.Web/Components/ChatComponent.razor.css`)

Created component-specific CSS for the content safety selector:
- Clean, modern design consistent with the rest of the UI
- Hover and focus states for better UX
- Proper disabled state styling
- Responsive layout

## Behavior

### Disabled Mode (Default)
- No content safety checks are performed
- Fastest response time
- No API calls to Content Safety service

### Audit Mode
- Checks both user prompts and MCP tool outputs for jailbreak attempts
- **If jailbreak detected:** Continues processing and prepends warning to response
  - User prompt: `⚠️ [AUDIT] Jailbreak detected in user prompt`
  - Tool outputs: `⚠️ [AUDIT] Jailbreak detected in tool outputs`
- Logs warnings for security monitoring
- Full response is still returned to user

### Enforce Mode
- Checks both user prompts and MCP tool outputs for jailbreak attempts
- **If jailbreak detected:** Blocks the response immediately
  - User prompt: Returns error message asking user to rephrase
  - Tool outputs: Returns security alert with offending text
- No response content is returned to user
- Provides maximum security

## Configuration Fallback

The `local.settings.json` still contains the default configuration:
```json
"AIServicesContentSafetyJailbreakDetectionMode": "Audit"
```

This value is used when:
- No `ContentSafetyMode` is specified in the request
- An invalid `ContentSafetyMode` value is provided

## Testing

To test the feature:

1. **Test Disabled Mode:**
   - Set dropdown to "Disabled"
   - Send any message (including potential jailbreak attempts)
   - Should process normally with no content safety checks

2. **Test Audit Mode:**
   - Set dropdown to "Audit"
   - Send a benign message → Should work normally
   - Send a jailbreak attempt → Should show warning but continue

3. **Test Enforce Mode:**
   - Set dropdown to "Enforce"
   - Send a benign message → Should work normally
   - Send a jailbreak attempt → Should block with error message

4. **Test Configuration Fallback:**
   - Leave the configuration at "Audit"
   - Don't set the dropdown (or set to invalid value)
   - Should use "Audit" mode from configuration

## Benefits

1. **Flexibility:** Users can adjust security level based on their needs
2. **Testing:** Easy to test different security modes without redeployment
3. **Transparency:** Clear indication of which mode is active
4. **Fallback:** Safe defaults ensure security even if UI value isn't set
5. **Logging:** Full audit trail of which mode was used for each request
