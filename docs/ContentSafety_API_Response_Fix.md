# Content Safety API Response Format Fix

## Issue

The Azure Content Safety API response format was incorrectly mapped in the code. The actual API response uses `"attackDetected"` as the property name, not `"detected"`.

## Actual API Response Format

```json
{
  "userPromptAnalysis": {
    "attackDetected": false
  },
  "documentsAnalysis": [
    {
      "attackDetected": false
    }
  ]
}
```

## Fix Applied

Updated the JSON property names in the response model classes:

### Before (Incorrect)
```csharp
private class JailbreakAnalysis
{
    [JsonPropertyName("detected")]
    public bool Detected { get; set; }
}

private class DocumentAnalysis
{
    [JsonPropertyName("detected")]
    public bool Detected { get; set; }
}
```

### After (Correct)
```csharp
private class JailbreakAnalysis
{
    [JsonPropertyName("attackDetected")]
    public bool Detected { get; set; }
}

private class DocumentAnalysis
{
    [JsonPropertyName("attackDetected")]
    public bool Detected { get; set; }
}
```

## Changes Made

1. **ContentSafetyService.cs**
   - Updated `JailbreakAnalysis` class: `[JsonPropertyName("detected")]` ? `[JsonPropertyName("attackDetected")]`
   - Updated `DocumentAnalysis` class: `[JsonPropertyName("detected")]` ? `[JsonPropertyName("attackDetected")]`

2. **ContentSafetyServiceTests.cs**
   - Updated all test mock responses to use `"attackDetected"` instead of `"detected"`
   - Tests affected:
     - `DetectJailbreakAsync_WithJailbreakDetected_ReturnsTrue`
     - `DetectJailbreakAsync_WithNoJailbreakDetected_ReturnsFalse`
     - `DetectJailbreakAsync_WithLongText_SplitsIntoChunks`
     - `DetectJailbreakAsync_WithCancellationToken_PassesTokenToHttpClient`

## Impact

- **Behavior**: The service will now correctly parse the Azure Content Safety API responses
- **Breaking Changes**: None (internal implementation detail)
- **Tests**: All tests updated to match the correct API response format

## Testing

? Build successful  
? All tests should now correctly validate against the actual API response format

## Reference

Azure AI Content Safety Prompt Shield API documentation uses `attackDetected` as the field name in the response.

---

**Date**: January 2025  
**Type**: Bug Fix  
**Severity**: High (would cause all detections to fail)
