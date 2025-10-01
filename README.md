# ElasticOn Risk Agent Demo

ElasticOn Risk Agent Demo is a .NET 9 solution that demonstrates PDF document processing capabilities for risk assessment workflows. The solution includes Azure Functions for PDF intake, comprehensive unit tests, and a placeholder for future agent framework integration.

## Solution Structure

- `ElasticOn.RiskAgent.Demo.sln` – root solution file
- `src/ElasticOn.RiskAgent.Demo.Functions` – Azure Functions isolated worker app with PDF processing capabilities
- `src/ElasticOn.RiskAgent.Demo.Agent` – placeholder Microsoft Agent Framework project (implementation coming soon)
- `tests/ElasticOn.RiskAgent.Demo.Functions.Tests` – comprehensive unit tests for the Functions project

## Azure Functions Project (`ElasticOn.RiskAgent.Demo.Functions`)

### Purpose
The Functions project provides a cloud-ready HTTP API for processing PDF documents with associated metadata. It's designed to handle PDF document intake for risk assessment workflows.

### Key Components

#### `ProcessPdfFunction`
- **Endpoint**: `POST /api/process-pdf`
- **Purpose**: Accepts PDF documents encoded as Base64 strings along with structured metadata
- **Processing**: Validates input, decodes PDF content, extracts text from all pages, performs text chunking with configurable overlap, and processes metadata as strongly-typed objects
- **Response**: Returns structured information about the processed document including chunking statistics

#### `IndexDocumentFunction`
- **Endpoint**: `POST /api/index-document`
- **Purpose**: Indexes document chunks with metadata into Elasticsearch
- **Processing**: Accepts document chunk data and stores it in Elasticsearch for search and retrieval
- **Response**: Returns document ID and indexing status

#### `ProcessPdfParser` (Service)
- **Purpose**: Orchestrates PDF processing workflow including Base64 decoding, text extraction, and chunking
- **Features**: 
  - Robust Base64 decoding with URL-safe character handling
  - Integration with PdfTextExtractor for text extraction
  - Integration with TextChunkingService for text chunking
  - Configurable chunk size and overlap settings
  - Document metadata validation and processing
  - Strong typing for document metadata objects

#### `PdfTextExtractor` (Service)
- **Purpose**: Extracts text content from PDF documents
- **Features**:
  - Extracts text from all pages of a PDF document
  - Multiple extraction methods for robustness (built-in text, words, letters, raw content streams)
  - Graceful error handling for PDFs with font loading issues
  - Lenient parsing options for malformed PDFs

#### `TextChunkingService` (Service)
- **Purpose**: Chunks text into overlapping segments for improved context in downstream processing
- **Features**:
  - Configurable chunk size (default: 500 characters)
  - Configurable overlap size (default: 50 characters)
  - Generates chunking statistics (page count, chunks per page, min/max/average)
  - Validates input parameters to ensure chunk size is greater than overlap

#### `ElasticsearchService` (Service)
- **Purpose**: Manages document indexing to Elasticsearch
- **Features**:
  - Configurable Elasticsearch connection (URI, API key, index name)
  - Document ID generation based on filename, page number, and chunk number
  - Automatic index creation if not exists
  - Comprehensive error handling and logging

#### `DocumentMetadata` (Model)
- **Purpose**: Strongly-typed representation of document metadata
- **Features**: Essential document properties including ID, filename, path, version, timestamps, and link information

#### `ChunkingStats` (Model)
- **Purpose**: Statistics about text chunking results
- **Features**: Page count, chunks per page array, average/max/min chunks per page

### Dependencies
- **Microsoft.Azure.Functions.Worker** (2.1.0) - Core Azure Functions isolated worker runtime
- **Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore** (2.0.2) - HTTP trigger support with ASP.NET Core integration
- **Microsoft.Azure.Functions.Worker.Extensions.OpenApi** (1.5.1) - OpenAPI/Swagger support for Azure Functions
- **Microsoft.ApplicationInsights.WorkerService** (2.23.0) - Application Insights telemetry and logging
- **Microsoft.Azure.Functions.Worker.ApplicationInsights** (2.0.0) - Application Insights integration for Functions
- **Microsoft.Azure.Functions.Worker.Sdk** (2.0.5) - Build-time SDK for Functions
- **PdfPig** (0.1.11) - PDF text extraction and manipulation
- **Elastic.Clients.Elasticsearch** (9.1.7) - Official Elasticsearch .NET client for document indexing

### Example Request

```http
POST http://localhost:7071/api/process-pdf
Content-Type: application/json

{
  "fileContent": "JVBERi0xLjc...",
  "metadata": {
    "id": "sample-doc-001",
    "filenameWithExtension": "sample-doc.pdf",
    "fullPath": "Risk Assessments/sample-doc.pdf",
    "versionNumber": "1.0",
    "modified": "2025-09-29T10:00:00Z",
    "created": "2025-09-29T09:00:00Z",
    "link": "https://example.sharepoint.com/documents/sample-doc.pdf"
  }
}
```

### Response Format
```json
{
  "message": "PDF accepted for processing.",
  "size": 1024,
  "metadata": { /* parsed metadata object */ },
  "document": {
    "id": "sample-doc-001",
    "filenameWithExtension": "sample-doc.pdf",
    "versionNumber": "1.0",
    "pageCount": 5,
    "averageChunksPerPage": 3.2,
    "maxChunksPerPage": 5,
    "minChunksPerPage": 2
  }
}
```

### Configuration

The PDF processing function supports the following configurable parameters that can be set in `local.settings.json`:

#### Text Chunking Configuration
- **ChunkSize** (default: 500) - Size of each text chunk in characters
- **ChunkOverlap** (default: 50) - Number of characters to overlap between consecutive chunks

Chunking with overlap helps maintain context across chunk boundaries, which is important for downstream processing like semantic search or LLM analysis.

#### Function Integration Configuration
- **IndexDocumentUrlPath** (default: "/api/index-document") - URL path for the IndexDocument function endpoint. Allows customization of the internal function URL used when ProcessPDF automatically indexes chunks.
- **IndexDocumentMaxRetries** (default: 3) - Maximum number of retry attempts when calling IndexDocument fails due to transient errors. Retries are performed for 5xx server errors and 429 rate limit responses.
- **IndexDocumentInitialRetryDelayMs** (default: 1000) - Initial delay in milliseconds before the first retry. Subsequent retries use exponential backoff (delay doubles with each attempt).

### Elasticsearch Integration

The solution includes Elasticsearch integration for indexing processed document chunks:

- **ElasticsearchService** - Handles connection and indexing operations
- **IndexDocumentFunction** - Provides HTTP API endpoint for indexing document chunks
- **Configuration** - Supports configurable Elasticsearch URI, API key, and index name
- **Document ID Generation** - Automatic generation of unique IDs based on filename, page number, and chunk number

For detailed information about the IndexDocument function, see [docs/IndexDocumentFunction-README.md](docs/IndexDocumentFunction-README.md).

## Tests Project (`ElasticOn.RiskAgent.Demo.Functions.Tests`)

### Purpose
Provides comprehensive unit testing for the Functions project, ensuring reliability and correctness of PDF processing logic.

### Test Coverage

#### `ProcessPdfParserTests`
- **PDF Validation**: Verifies successful Base64 decoding and PDF format validation
- **Text Extraction**: Tests PDF text extraction from all pages
- **Text Chunking**: Validates text chunking with configurable chunk size and overlap
- **Chunking Statistics**: Verifies correct calculation of page count, chunks per page, and min/max/average statistics
- **Metadata Processing**: Tests processing of metadata objects as strongly-typed `DocumentMetadata` instances  
- **Error Handling**: Validates proper exception handling for malformed input

### Test Features
- **Sample Data**: Uses realistic test data from `samplePdfFileRequest.json`
- **PDF Verification**: Validates decoded PDF content starts with proper PDF header (`%PDF-`)
- **Text Extraction Verification**: Ensures text is correctly extracted from PDF pages
- **Chunking Validation**: Verifies text is properly chunked with correct overlap behavior
- **Statistics Validation**: Ensures chunking statistics accurately reflect the chunking results
- **Metadata Mapping**: Ensures all essential document metadata fields are correctly parsed and accessible

### Dependencies
- **xunit** (2.9.2) - Primary testing framework
- **Microsoft.NET.Test.Sdk** (17.12.0) - .NET test SDK and runner
- **xunit.runner.visualstudio** (2.8.2) - Visual Studio test runner integration
- **coverlet.collector** (6.0.2) - Code coverage collection
- **UglyToad.PdfPig** (0.1.8) - PDF manipulation and validation (for future enhancements)

### Test Data
The tests utilize a sample PDF request file (`samplePdfFileRequest.json`) that contains essential document metadata and Base64-encoded PDF content.

## Prerequisites

- .NET SDK 9 preview or later.
- Azure Functions Core Tools v4 when running the function locally.

## Local development

Restore dependencies and build the solution:

```bash
dotnet build ElasticOn.RiskAgent.Demo.sln
```

To run the Azure Functions host locally:

```bash
cd src/ElasticOn.RiskAgent.Demo.Functions
func start
```

> `local.settings.json` contains only development defaults and is excluded from version control. Provide your own values as needed.

## Azure Deployment

This project includes Azure Developer CLI (azd) support for easy deployment to Azure Functions with Flex Consumption plan.

### Quick Start

1. **Setup Environment**: Run the automated setup script
   ```bash
   ./scripts/setup-azd-environment.sh
   ```

2. **Deploy to Azure**: Deploy infrastructure and application
   ```bash
   azd up
   ```

3. **Configure Elasticsearch**: Update your Elasticsearch connection details
   ```bash
   ./scripts/update-elasticsearch-secrets.sh
   ```

### Deployment with SAS Token Restrictions

If your organization blocks SAS token-based storage access, use the alternative deployment method:

```bash
# 1. Provision infrastructure only
azd provision

# 2. Deploy without SAS tokens
./scripts/deploy-without-sas.sh

# 3. Configure post-deployment
./scripts/configure-function-key.sh
./scripts/update-elasticsearch-secrets.sh
```

See [DEPLOYMENT_WITHOUT_SAS.md](docs/DEPLOYMENT_WITHOUT_SAS.md) for detailed information.

### What Gets Deployed

- **Azure Functions App** (Flex Consumption plan)
- **Azure OpenAI Service** with text-embedding-ada-002 model
- **Application Insights** for monitoring and logging
- **Storage Account** with managed identity authentication

> **Note**: Azure Key Vault is not currently used due to Flex Consumption plan limitations.  
> Secrets are stored directly in Function App settings. See [TODO_KEYVAULT_INTEGRATION.md](docs/TODO_KEYVAULT_INTEGRATION.md) for future plans.

For detailed deployment instructions, see [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md).

## Next steps

- Flesh out the Microsoft Agent Framework app in `ElasticOn.RiskAgent.Demo.Agent`.
- Connect the function output to the agent workflow.
- Scale the deployment for production workloads.
