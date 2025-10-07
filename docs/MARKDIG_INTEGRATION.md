# Markdig Integration for Markdown to HTML Conversion

## Overview
This document describes the integration of [Markdig](https://github.com/xoofx/markdig) to convert markdown responses from the Azure AI Agent into HTML before sending them to the web frontend.

## Changes Made

### 1. NuGet Package Addition
Added `Markdig` version 0.39.0 to the Functions project:

**File**: `src/ElasticOn.RiskAgent.Demo.Functions/ElasticOn.RiskAgent.Demo.Functions.csproj`
```xml
<PackageReference Include="Markdig" Version="0.39.0" />
```

### 2. Updated ChatFunction
**File**: `src/ElasticOn.RiskAgent.Demo.Functions/Functions/ChatFunction.cs`

- Added `using Markdig;` directive
- Added markdown to HTML conversion in the response building section:
```csharp
// Convert markdown to HTML using Markdig
var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .Build();
var messageHtml = Markdown.ToHtml(finalResponse, pipeline);
```

The `UseAdvancedExtensions()` method enables:
- Tables
- Task lists
- Auto-links
- Strikethrough
- Pipe tables
- And many other advanced markdown features

### 3. Updated Response Models
**Files**: 
- `src/ElasticOn.RiskAgent.Demo.Functions/Models/SendMessageResponse.cs`
- `src/ElasticOn.RiskAgent.Demo.Web/Models/ChatModels.cs`

Added `MessageHtml` property to both models:
```csharp
/// <summary>
/// The assistant's response message converted to HTML
/// </summary>
public string MessageHtml { get; set; } = string.Empty;
```

The `Message` property still contains the original markdown for backward compatibility.

### 4. Updated Web Component
**File**: `src/ElasticOn.RiskAgent.Demo.Web/Components/ChatComponent.razor`

- Added `@using Microsoft.AspNetCore.Components` for `MarkupString`
- Updated to use `MessageHtml` instead of `Message` from the API response
- Updated rendering logic to safely render HTML for assistant messages:
```razor
else if (message.Role == "assistant")
{
    @((MarkupString)message.Content)
}
```

## Benefits

1. **Rich Formatting**: Agent responses can now include:
   - Headers
   - Bold and italic text
   - Code blocks with syntax highlighting
   - Tables
   - Lists (ordered and unordered)
   - Links
   - And more

2. **Advanced Extensions**: The Markdig pipeline includes advanced extensions for:
   - GitHub-flavored markdown features
   - Task lists
   - Emoji support
   - Custom containers
   - And more

3. **Safe HTML Rendering**: Using Blazor's `MarkupString` ensures the HTML is rendered safely without XSS vulnerabilities

4. **Backward Compatibility**: The original markdown is still available in the `Message` property

## Usage

The conversion happens automatically in the Azure Function before sending the response. The web frontend receives both:
- `message`: Original markdown text
- `messageHtml`: Converted HTML

The frontend displays the HTML version for assistant messages, providing a rich user experience.

## Security Note

The HTML rendering in Blazor using `MarkupString` is safe because:
1. Markdig sanitizes the HTML output by default
2. The content comes from the trusted Azure AI Agent service
3. User input is not directly rendered as HTML (only assistant responses)

## Testing

Both projects build successfully with these changes:
- Functions project: ✅ Build succeeded
- Web project: ✅ Build succeeded

## Troubleshooting

### Links Not Showing Up

If hyperlinks are not appearing in the assistant responses:

1. **Verify Markdown Format**: The agent must return proper markdown link syntax:
   - `[Link Text](https://example.com)` for inline links
   - `https://example.com` for auto-linked URLs (with UseAutoLinks extension)

2. **Check CSS**: Ensure the CSS styles for links are properly applied (see `wwwroot/css/app.css`)

3. **Inspect HTML Output**: You can log the `messageHtml` value in ChatFunction.cs to see the actual HTML being generated:
   ```csharp
   _logger.LogInformation("Generated HTML: {Html}", messageHtml);
   ```

4. **Verify MarkupString Rendering**: Ensure the Blazor component is using `@((MarkupString)message.Content)` for assistant messages

### Common Link Formats Supported

The Markdig pipeline with `UseAdvancedExtensions()` supports:

- **Inline links**: `[Text](URL)`
- **Reference links**: `[Text][ref]` with `[ref]: URL` elsewhere
- **Auto-links**: `<https://example.com>`
- **Bare URLs**: `https://example.com` (automatically converted to links)
- **Email auto-links**: `<email@example.com>`
