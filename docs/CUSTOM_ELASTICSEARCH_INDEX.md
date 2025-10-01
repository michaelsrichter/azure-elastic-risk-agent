# Custom Elasticsearch Index Name Feature

## Overview

The `ProcessPdfFunction` now supports specifying a custom Elasticsearch index name directly in the request payload, making it easier to route documents to different indexes without providing the full Elasticsearch configuration.

## Changes Made

### 1. ProcessPdfRequest Model
Added a new property `ElasticsearchIndexName` to the request payload:

```csharp
[JsonPropertyName("elasticsearchIndexName")]
public string? ElasticsearchIndexName { get; init; }
```

### 2. ProcessPdfFunction Logic
When `ElasticsearchIndexName` is provided in the request:
- It takes precedence over `ElasticsearchConfig.IndexName`
- Creates or updates the `ElasticsearchConfig` with the custom index name
- Preserves any other Elasticsearch configuration (URI, API key) if provided

### 3. Response Enhancement
The response now includes the effective index name used for indexing:

```json
{
  "elasticsearchConfig": {
    "hasCustomConfig": true,
    "indexName": "your-custom-index-name",
    "uri": null
  }
}
```

## Usage Examples

### Simple Approach: Just Specify Index Name
```json
POST http://localhost:7071/api/process-pdf
Content-Type: application/json

{
  "fileContent": "<base64-encoded-pdf>",
  "metadata": {
    "id": "doc-123",
    "filenameWithExtension": "document.pdf",
    "versionNumber": "1.0"
  },
  "elasticsearchIndexName": "my-custom-index",
  "indexDocument": true
}
```

This will use:
- **Index Name**: `my-custom-index` (from request)
- **URI**: Default from `local.settings.json`
- **API Key**: Default from `local.settings.json`

### Full Control: Specify Complete Config
```json
POST http://localhost:7071/api/process-pdf
Content-Type: application/json

{
  "fileContent": "<base64-encoded-pdf>",
  "metadata": {
    "id": "doc-456",
    "filenameWithExtension": "document.pdf",
    "versionNumber": "1.0"
  },
  "elasticsearchConfig": {
    "uri": "https://my-elastic.example.com:443",
    "apiKey": "your-api-key",
    "indexName": "another-index"
  },
  "indexDocument": true
}
```

### Override Index Name in Existing Config
```json
POST http://localhost:7071/api/process-pdf
Content-Type: application/json

{
  "fileContent": "<base64-encoded-pdf>",
  "metadata": {
    "id": "doc-789",
    "filenameWithExtension": "document.pdf",
    "versionNumber": "1.0"
  },
  "elasticsearchConfig": {
    "uri": "https://my-elastic.example.com:443",
    "apiKey": "your-api-key",
    "indexName": "this-will-be-overridden"
  },
  "elasticsearchIndexName": "priority-index",
  "indexDocument": true
}
```

In this case, `elasticsearchIndexName` takes precedence, so the document will be indexed to `priority-index`.

## Precedence Rules

1. **`elasticsearchIndexName`** (highest priority) - Simple string property
2. **`elasticsearchConfig.indexName`** - Part of full config object
3. **Default from settings** (lowest priority) - `ElasticsearchIndexName` in `local.settings.json`

## Implementation Flow

```
ProcessPdfFunction receives request
  ↓
If elasticsearchIndexName is provided:
  - Create/update ElasticsearchConfig with this index name
  - Preserve other config properties (URI, API key)
  ↓
Pass effective config to ProcessPdfParser
  ↓
ProcessPdfParser extracts text and chunks
  ↓
Each chunk is sent to IndexDocumentFunction
  ↓
IndexDocumentFunction indexes to the specified index
```

## Benefits

1. **Simpler API**: Just specify the index name without full config
2. **Flexibility**: Can still use full config when needed
3. **Clear precedence**: Easy to understand which index will be used
4. **Backward compatible**: Existing code using `elasticsearchConfig` continues to work

## Testing

Test examples are available in `process-pdf-custom-index.http`.

### Verify Index Usage
Check the function logs to confirm which index was used:

```bash
# Look for log entries like:
# "Using custom Elasticsearch configuration - Uri: default, Index: my-custom-index"
# "Indexing document with ID: ... using custom config to index my-custom-index"
```

### Response Confirmation
The response will show the effective index name:

```json
{
  "message": "PDF accepted for processing and indexing.",
  "elasticsearchConfig": {
    "hasCustomConfig": true,
    "indexName": "my-custom-index",
    "uri": null
  }
}
```

## Files Modified

1. **ProcessPdfFunction.cs**
   - Added `ElasticsearchIndexName` property to `ProcessPdfRequest`
   - Added logic to merge index name into config
   - Updated response to show effective index name

2. **process-pdf-custom-index.http** (new)
   - Example HTTP requests demonstrating the feature

3. **docs/CUSTOM_ELASTICSEARCH_INDEX.md** (this file)
   - Complete documentation of the feature
