# IndexDocument Azure Function

## Overview

The `IndexDocument` Azure Function accepts document chunk data and indexes it into Elasticsearch. This function is designed to process PDF document chunks that have been extracted and need to be stored for search and retrieval.

## Endpoint

- **URL**: `POST /api/index-document`
- **Authorization Level**: Function (requires function key)

## Request Format

The function expects a JSON payload with the following structure:

```json
{
  "documentMetadata": {
    "id": "sample-doc-001",
    "filenameWithExtension": "document.pdf",
    "fullPath": "/path/to/document.pdf",
    "versionNumber": "1.0",
    "modified": "2025-09-28T10:30:00Z",
    "created": "2025-09-28T10:00:00Z",
    "link": "https://example.sharepoint.com/documents/document.pdf"
  },
  "pageNumber": 1,
  "pageChunkNumber": 1,
  "chunk": "This is the extracted text content from the document chunk."
}
```

### Required Fields

- `documentMetadata.filenameWithExtension`: The filename with extension (used for ID generation)
- `pageNumber`: Page number of the document (integer)
- `pageChunkNumber`: Chunk number within the page (integer)
- `chunk`: The extracted text content (string)

### Optional Fields

All other fields in `documentMetadata` are optional but recommended for better document management and search functionality:
- `id`: Unique document identifier
- `fullPath`: Full path to the document
- `versionNumber`: Document version
- `modified`: Last modified timestamp
- `created`: Creation timestamp
- `link`: Link to the original document

## Response Format

### Success Response (200 OK)
```json
{
  "success": true,
  "documentId": "a1b2c3d4e5f6...", 
  "message": "Document successfully indexed"
}
```

### Error Responses

#### Bad Request (400)
- Empty request body
- Missing or invalid `documentMetadata`
- Missing `filenameWithExtension`
- Invalid JSON format

#### Internal Server Error (500)
- Elasticsearch indexing failure
- Unexpected server errors

## Elasticsearch Index Structure

The function creates documents in Elasticsearch with the following fields:

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Generated unique identifier (SHA256 hash of filename + page + chunk numbers) |
| `filenameWithExtension` | string | Original filename with extension |
| `fullPath` | string | Full path to the document |
| `versionNumber` | string | Document version |
| `modified` | datetime | Last modified timestamp |
| `created` | datetime | Creation timestamp |
| `link` | string | Link to the original document |
| `pageNumber` | integer | Page number |
| `pageChunkNumber` | integer | Chunk number within the page |
| `chunk` | string | Extracted text content |

## ID Generation Logic

The document ID is generated using SHA256 hash of the combination:
`{filenameWithExtension}_{pageNumber}_{pageChunkNumber}`

This ensures:
- Unique IDs for each chunk
- Consistent IDs for the same document chunk
- Automatic overwriting of existing documents with the same ID

## Configuration

The function uses the following configuration settings from `local.settings.json`:

```json
{
  "Values": {
    "ElasticsearchUri": "http://localhost:9200",
    "ElasticsearchApiKey": "your-api-key-here",
    "ElasticsearchIndexName": "risk-agent-documents"
  }
}
```

### Configuration Settings

- `ElasticsearchUri`: Elasticsearch cluster endpoint
- `ElasticsearchApiKey`: API key for authentication (optional, but recommended for production)
  - Generate API keys in Elasticsearch using: `POST /_security/api_key` 
  - Or use Kibana UI: Stack Management > API Keys
- `ElasticsearchIndexName`: Target index name (defaults to "risk-agent-documents")

## Testing

Use the provided `index-document.http` file to test the function locally:

1. Start the Azure Functions runtime: `func start`
2. Send POST requests to `http://localhost:7071/api/index-document`
3. Use VS Code REST Client extension or curl to send test requests
4. Verify documents are indexed in Elasticsearch

## Index Management

The function automatically handles Elasticsearch index management:

- **Index Creation**: If the specified index doesn't exist, it will be created automatically
- **Index Settings**: Created with 1 shard and 0 replicas (suitable for development/single-node)
- **Dynamic Mapping**: Elasticsearch will automatically infer field types from the document structure
- **Upsert Behavior**: Documents with the same ID will be overwritten

### Automatic Index Creation

When a document is indexed, the function will:

1. Check if the target index exists
2. If not, create the index with basic settings
3. Proceed with document indexing
4. Log all operations for monitoring

## Dependencies

- `Elastic.Clients.Elasticsearch` (9.1.7): Official Elasticsearch .NET client
- `Microsoft.Azure.Functions.Worker.*`: Azure Functions v4 isolated runtime

## Error Handling

The function includes comprehensive error handling:
- JSON deserialization errors
- Validation of required fields
- Elasticsearch connection and indexing errors
- Proper HTTP status codes and error messages
- Structured logging for debugging

## Logging

The function logs important events:
- Document processing start/completion
- Generated document IDs
- Elasticsearch indexing success/failure
- Error conditions with details

Use Application Insights or local logs to monitor function execution.