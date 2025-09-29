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
- **Processing**: Validates input, decodes PDF content, processes metadata as strongly-typed objects
- **Response**: Returns structured information about the processed document

#### `ProcessPdfParser` (Service)
- **Purpose**: Handles Base64 decoding and metadata processing
- **Features**: 
  - Robust Base64 decoding with URL-safe character handling
  - Document metadata validation and processing
  - Strong typing for document metadata objects

#### `DocumentMetadata` (Model)
- **Purpose**: Strongly-typed representation of document metadata
- **Features**: Essential document properties including ID, filename, path, version, timestamps, and link information

### Dependencies
- **Microsoft.Azure.Functions.Worker** (2.1.0) - Core Azure Functions isolated worker runtime
- **Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore** (2.0.2) - HTTP trigger support with ASP.NET Core integration
- **Microsoft.ApplicationInsights.WorkerService** (2.23.0) - Application Insights telemetry and logging
- **Microsoft.Azure.Functions.Worker.ApplicationInsights** (2.0.0) - Application Insights integration for Functions
- **Microsoft.Azure.Functions.Worker.Sdk** (2.0.5) - Build-time SDK for Functions

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
    "versionNumber": "1.0"
  }
}
```

## Tests Project (`ElasticOn.RiskAgent.Demo.Functions.Tests`)

### Purpose
Provides comprehensive unit testing for the Functions project, ensuring reliability and correctness of PDF processing logic.

### Test Coverage

#### `ProcessPdfParserTests`
- **PDF Validation**: Verifies successful Base64 decoding and PDF format validation
- **Metadata Processing**: Tests processing of metadata objects as strongly-typed `DocumentMetadata` instances  
- **Error Handling**: Validates proper exception handling for malformed input

### Test Features
- **Sample Data**: Uses realistic test data from `samplePdfFileRequest.json`
- **PDF Verification**: Validates decoded PDF content starts with proper PDF header (`%PDF-`)
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

## Next steps

- Flesh out the Microsoft Agent Framework app in `ElasticOn.RiskAgent.Demo.Agent`.
- Connect the function output to the agent workflow.
