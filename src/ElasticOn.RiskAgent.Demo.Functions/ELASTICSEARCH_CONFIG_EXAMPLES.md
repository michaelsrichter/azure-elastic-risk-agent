# Testing Elasticsearch Config Override

## Example 1: IndexDocument with default Elasticsearch settings
POST /api/index-document
```json
{
  "documentMetadata": {
    "filenameWithExtension": "sample.pdf",
    "title": "Sample Document",
    "author": "Test Author"
  },
  "pageNumber": 1,
  "pageChunkNumber": 1,
  "chunk": "This is a sample document chunk."
}
```

## Example 2: IndexDocument with custom Elasticsearch settings
POST /api/index-document
```json
{
  "documentMetadata": {
    "filenameWithExtension": "sample.pdf",
    "title": "Sample Document",
    "author": "Test Author"
  },
  "pageNumber": 1,
  "pageChunkNumber": 1,
  "chunk": "This is a sample document chunk.",
  "elasticsearchConfig": {
    "uri": "http://custom-elasticsearch:9200",
    "apiKey": "custom-api-key",
    "indexName": "custom-index-name"
  }
}
```

## Example 3: IndexDocument with partial Elasticsearch config (will fallback to defaults for missing values)
POST /api/index-document
```json
{
  "documentMetadata": {
    "filenameWithExtension": "sample.pdf",
    "title": "Sample Document",
    "author": "Test Author"
  },
  "pageNumber": 1,
  "pageChunkNumber": 1,
  "chunk": "This is a sample document chunk.",
  "elasticsearchConfig": {
    "indexName": "special-documents-index"
  }
}
```

## Example 4: ProcessPDF with custom Elasticsearch settings (for future use by consumers)
POST /api/process-pdf
```json
{
  "fileContent": "base64-encoded-pdf-content-here",
  "metadata": {
    "filenameWithExtension": "document.pdf",
    "title": "PDF Document",
    "author": "PDF Author"
  },
  "elasticsearchConfig": {
    "uri": "http://custom-elasticsearch:9200",
    "indexName": "processed-documents"
  }
}
```

## Configuration Fallback Behavior

- If `elasticsearchConfig` is not provided: Uses values from `local.settings.json`
- If `elasticsearchConfig` is provided but some fields are missing: Missing fields fallback to `local.settings.json` values
- If `elasticsearchConfig` is provided with all fields: Uses only the provided values

## Current default values from local.settings.json:
- URI: `http://localhost:9200`
- API Key: `""` (empty)
- Index Name: `risk-agent-documents`