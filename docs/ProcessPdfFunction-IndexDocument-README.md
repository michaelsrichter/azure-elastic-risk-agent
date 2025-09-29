# ProcessPdfFunction with Elasticsearch Indexing

The ProcessPdfFunction has been enhanced to support automatic indexing of document chunks to Elasticsearch. This feature allows you to both process PDFs for text extraction and simultaneously index the content for search and retrieval.

## New Features

### 1. Optional Document Indexing

You can now control whether PDF chunks should be automatically indexed to Elasticsearch by setting the `indexDocument` field in your request.

### 2. Enhanced Response Information

The response now includes information about whether indexing was performed and details about the Elasticsearch configuration used.

## Request Schema

```json
{
  "fileContent": "base64-encoded-pdf-content",
  "metadata": {
    "id": "unique-document-id",
    "filenameWithExtension": "document.pdf",
    "fullPath": "/path/to/document.pdf",
    "versionNumber": "1.0"
  },
  "indexDocument": true,  // NEW: Set to true to enable chunk indexing
  "elasticsearchConfig": {  // Optional: Custom Elasticsearch configuration
    "uri": "http://localhost:9200",
    "indexName": "custom-index-name",
    "apiKey": "your-api-key"
  }
}
```

## Response Schema

```json
{
  "message": "PDF accepted for processing and indexing.",
  "size": 12345,
  "metadata": { /* document metadata */ },
  "indexingEnabled": true,  // NEW: Indicates if indexing was performed
  "elasticsearchConfig": {  // NEW: Shows configuration used
    "hasCustomConfig": true,
    "indexName": "custom-index-name",
    "uri": "http://localhost:9200"
  },
  "document": {
    "id": "unique-document-id",
    "filenameWithExtension": "document.pdf",
    "versionNumber": "1.0",
    "pageCount": 5,
    "averageChunksPerPage": 3.2,
    "maxChunksPerPage": 4,
    "minChunksPerPage": 2
  }
}
```

## Usage Examples

### 1. Process PDF with Indexing

```bash
curl -X POST "http://localhost:7071/api/process-pdf" \
  -H "Content-Type: application/json" \
  -d '{
    "fileContent": "JVBERi0xLjQK...",
    "metadata": {
      "id": "doc-001",
      "filenameWithExtension": "report.pdf",
      "fullPath": "/uploads/report.pdf",
      "versionNumber": "1.0"
    },
    "indexDocument": true,
    "elasticsearchConfig": {
      "uri": "http://localhost:9200",
      "indexName": "documents",
      "apiKey": "your-api-key"
    }
  }'
```

### 2. Process PDF without Indexing (Legacy Mode)

```bash
curl -X POST "http://localhost:7071/api/process-pdf" \
  -H "Content-Type: application/json" \
  -d '{
    "fileContent": "JVBERi0xLjQK...",
    "metadata": {
      "id": "doc-002",
      "filenameWithExtension": "report.pdf",
      "fullPath": "/uploads/report.pdf",
      "versionNumber": "1.0"
    },
    "indexDocument": false
  }'
```

### 3. Process PDF with Default Settings

```bash
curl -X POST "http://localhost:7071/api/process-pdf" \
  -H "Content-Type: application/json" \
  -d '{
    "fileContent": "JVBERi0xLjQK...",
    "metadata": {
      "id": "doc-003",
      "filenameWithExtension": "report.pdf",
      "fullPath": "/uploads/report.pdf",
      "versionNumber": "1.0"
    }
  }'
```

## How Indexing Works

When `indexDocument` is set to `true`:

1. **PDF Processing**: The PDF is processed normally, extracting text from all pages
2. **Text Chunking**: Each page's text is divided into smaller chunks using configurable chunk size and overlap
3. **Async Indexing**: Each chunk is asynchronously sent to the IndexDocumentFunction with metadata including:
   - Document metadata (ID, filename, path, version)
   - Page number (1-based)
   - Chunk number within the page (1-based)
   - The actual text content
   - Optional Elasticsearch configuration

4. **Non-blocking**: The indexing process runs asynchronously and doesn't block the PDF processing response

## Configuration

### Chunk Settings

The chunking behavior is controlled by configuration settings:

```json
{
  "ChunkSize": "500",     // Characters per chunk
  "ChunkOverlap": "50"    // Character overlap between chunks
}
```

### Elasticsearch Settings

Default Elasticsearch settings can be configured in the application settings:

```json
{
  "ElasticsearchUri": "http://localhost:9200",
  "ElasticsearchIndexName": "risk-agent-documents",
  "ElasticsearchApiKey": "your-default-api-key"
}
```

## Backward Compatibility

- **Existing clients**: All existing functionality continues to work unchanged
- **Default behavior**: `indexDocument` defaults to `false`, so no indexing occurs unless explicitly requested
- **Legacy requests**: Requests without the `indexDocument` field will work exactly as before

## Error Handling

- **PDF Processing Errors**: Are returned immediately to the client
- **Indexing Errors**: Are logged but don't affect the PDF processing response
- **HTTP Failures**: During indexing are handled gracefully without affecting the main operation

## Testing

The functionality includes comprehensive tests:

- **Unit Tests**: Verify the new field serialization and deserialization
- **Integration Tests**: Test the HTTP client interaction and chunk indexing behavior
- **Error Handling Tests**: Ensure robustness when indexing fails

Run tests with:
```bash
dotnet test --filter "ProcessPdf"
```

## Dependencies

The enhanced functionality requires:

- **HttpClient**: Registered in dependency injection for making HTTP calls
- **IndexDocumentFunction**: Must be available at the same base URL for indexing to work
- **Elasticsearch**: Must be accessible with the provided configuration

## Troubleshooting

### SSL Connection Issues

If you encounter SSL connection errors in local development:

```
Error calling IndexDocumentFunction: The SSL connection could not be established
```

This is handled automatically by:
1. Using HTTP (not HTTPS) for local development environments
2. Configuring the HttpClient to accept untrusted certificates in development mode
3. Automatic environment detection based on Azure Functions runtime variables

### Environment Detection

The system automatically detects the runtime environment:
- **Production**: Uses `https://{WEBSITE_HOSTNAME}` from Azure Functions environment
- **Local Development**: Uses `http://localhost:7071` to avoid SSL issues
- **Fallback**: Defaults to `http://localhost:7071` for unknown environments

### Common Issues

1. **Port Conflicts**: Ensure Azure Functions is running on port 7071 (default)
2. **Network Issues**: Verify the IndexDocumentFunction is accessible at the configured URL
3. **Authentication**: Ensure proper function keys are configured if using AuthorizationLevel.Function