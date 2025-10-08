# ElasticOn Risk Agent Demo

ElasticOn Risk Agent Demo is a comprehensive .NET 9 solution that demonstrates AI-powered risk assessment workflows using Azure AI, Elasticsearch, and Microsoft 365 integration. The solution provides PDF document processing, intelligent chat capabilities, and Teams bot integration for risk analysis.

## Solution Overview

This solution consists of three main applications and comprehensive testing infrastructure:

1. **Azure Functions** - Serverless API for document processing and chat interactions
2. **Microsoft 365 Teams Bot** - Teams-integrated bot for risk assessment queries
3. **Blazor WebAssembly App** - Modern web interface for risk analysis chat
4. **Comprehensive Testing** - Unit tests ensuring reliability and correctness

## High-Level Architecture

### Architecture Components

#### 1. Data Sources Layer
- **SharePoint**: Document libraries containing risk assessment documents, compliance reports, and financial documents
- **Triggers**: Power Automate monitors for new or updated files

#### 2. Orchestration Layer
- **Power Automate**: 
  - Monitors SharePoint for document changes
  - Extracts file properties (metadata)
  - Retrieves file content
  - Orchestrates calls to Azure Functions for processing

#### 3. Processing Layer (Azure Functions)
- **Process PDF Function**: Extracts text from PDF documents and chunks content for indexing
- **Index Document Function**: Stores processed documents in Elasticsearch with metadata
- **Chat Function**: Handles AI-powered conversational queries with context from Elasticsearch

#### 4. Search & Storage Layer
- **Elasticsearch (Azure Native)**: 
  - Hybrid search capabilities (vector + keyword)
  - Document storage with metadata
  - MCP server integration for AI agent tools
  - Secured via Azure Private Link

#### 5. AI Layer
- **Azure AI Foundry**: Agent orchestration framework
- **GPT-4o/GPT-4o-mini**: Language models for natural language understanding and generation
- **Azure Content Safety**: Jailbreak detection and content moderation
- **Elasticsearch MCP Tools**: Document search and retrieval capabilities for the AI agent

#### 6. User Interface Layer
- **Microsoft Teams**: Declarative agent for in-Teams risk assessment queries
- **Blazor WebAssembly**: Standalone web application for risk analysis chat
- **M365 Copilot**: Integration for enterprise-wide AI assistance

#### 7. Security Layer
- **Azure Private Link**: Secure connectivity between Azure services and Elasticsearch
- **API Management**: Centralized API gateway with authentication and rate limiting
- **Microsoft Entra ID**: Identity and access management for all client applications

### Data Flow

**Document Ingestion Flow:**
1. User uploads document to SharePoint
2. Power Automate detects new file and extracts properties
3. Power Automate sends file content to Process PDF Function
4. Function extracts text and creates chunks
5. Index Document Function stores chunks in Elasticsearch

**Query Flow:**
1. User submits query via Teams, Web, or M365 Copilot
2. Chat Function receives request and forwards to Azure AI Foundry Agent
3. Agent analyzes query and uses MCP tools to search Elasticsearch
4. Elasticsearch returns relevant document chunks
5. Agent generates contextual response using GPT-4o
6. Azure Content Safety validates response for safety
7. Response is returned to user interface

## Solution Structure

- `ElasticOn.RiskAgent.Demo.sln` – root solution file
- `src/ElasticOn.RiskAgent.Demo.Functions` – Azure Functions app with PDF processing, document indexing, and chat API
- `src/ElasticOn.RiskAgent.Demo.M365` – Microsoft Teams bot for risk assessment queries
- `src/ElasticOn.RiskAgent.Demo.Web` – Blazor WebAssembly chat interface
- `tests/ElasticOn.RiskAgent.Demo.Functions.Tests` – comprehensive unit tests for the Functions project
- `M365Agent` – Microsoft 365 Agents Toolkit configuration for Teams deployment

## 1. Azure Functions Project (`ElasticOn.RiskAgent.Demo.Functions`)

### Purpose
The Functions project provides a cloud-ready HTTP API for processing PDF documents, indexing them to Elasticsearch, and providing an intelligent chat interface powered by Azure AI Foundry. It serves as the backend for both the web application and Teams bot.

### Key Components

#### HTTP Functions

##### `ProcessPdfFunction`
- **Endpoint**: `POST /api/process-pdf`
- **Purpose**: Accepts PDF documents encoded as Base64 strings along with structured metadata
- **Processing**: Validates input, decodes PDF content, extracts text from all pages, performs text chunking with configurable overlap, and processes metadata as strongly-typed objects
- **Response**: Returns structured information about the processed document including chunking statistics

##### `IndexDocumentFunction`
- **Endpoint**: `POST /api/index-document`
- **Purpose**: Indexes document chunks with metadata into Elasticsearch
- **Processing**: Accepts document chunk data and stores it in Elasticsearch for search and retrieval
- **Response**: Returns document ID and indexing status

##### `ChatFunction`
- **Endpoint**: `POST /api/chat`
- **Purpose**: Provides an AI-powered chat interface for risk assessment queries
- **Features**:
  - Azure AI Foundry Agent integration with GPT-4 models
  - Elasticsearch MCP (Model Context Protocol) tool integration for document retrieval
  - Azure Content Safety for jailbreak detection and content moderation
  - Conversation state management across multiple turns
  - Streaming response support
- **Response**: Returns agent responses with referenced documents from Elasticsearch

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

#### Elasticsearch Configuration
- **ElasticsearchUri** (default: "http://localhost:9200") - The URI of your Elasticsearch cluster
- **ElasticsearchApiKey** - API key for authenticating with Elasticsearch (optional for local development)
- **ElasticsearchIndexName** (default: "risk-agent-documents") - The name of the index where documents will be stored
- **ElasticsearchMaxChunkSize** (default: 1000) - Maximum chunk size in tokens for Elasticsearch's semantic_text field chunking
- **ElasticsearchChunkingStrategy** (default: "none") - Chunking strategy for Elasticsearch semantic_text field (e.g., "none", "sentence", "word")
- **AzureOpenAiInferenceId** - The inference endpoint ID configured in Elasticsearch for Azure OpenAI embeddings

### Elasticsearch Integration

The solution includes Elasticsearch integration for indexing processed document chunks:

- **ElasticsearchService** - Handles connection and indexing operations
- **IndexDocumentFunction** - Provides HTTP API endpoint for indexing document chunks
- **Configuration** - Supports configurable Elasticsearch URI, API key, and index name
- **Document ID Generation** - Automatic generation of unique IDs based on filename, page number, and chunk number

For detailed information, see:
- [docs/IndexDocumentFunction-README.md](docs/IndexDocumentFunction-README.md) - IndexDocument function details
- [docs/ProcessPdfFunction-IndexDocument-README.md](docs/ProcessPdfFunction-IndexDocument-README.md) - ProcessPdf function details
- [docs/ChatFunction-README.md](docs/ChatFunction-README.md) - Chat function details
- [docs/Functions-ELASTICSEARCH_CONFIG_EXAMPLES.md](docs/Functions-ELASTICSEARCH_CONFIG_EXAMPLES.md) - Elasticsearch configuration examples

## 2. Microsoft Teams Bot (`ElasticOn.RiskAgent.Demo.M365`)

### Purpose
The M365 project provides a Microsoft Teams bot that integrates with Azure AI Foundry Agent for intelligent risk assessment conversations directly within Teams.

### Key Features

#### Teams Bot Integration
- **RiskAgentBot** - Teams Activity Handler for processing user messages
- **Conversation State Management** - Maintains agent and thread IDs across conversations
- **Adaptive Cards** - Rich message formatting with action buttons
- **Error Handling** - Graceful error messages and retry logic

#### AI Services
- **Azure AI Foundry Agent** - GPT-4 based agent for answering risk-related questions
- **Elasticsearch MCP Integration** - Retrieves relevant documents from Elasticsearch
- **Content Safety** - Azure Content Safety for jailbreak detection and content moderation
- **Configurable Detection Modes** - Disabled, Audit, or Enforce modes for content safety

#### Configuration
The bot requires configuration in `appsettings.json`:
- **AIServices:ProjectEndpoint** - Azure AI Foundry project endpoint
- **AIServices:ModelId** - Model deployment ID (e.g., "gpt-4.1-mini")
- **AIServices:MCPTool** - Elasticsearch MCP server configuration
- **ContentSafety** - Content Safety endpoint and subscription key
- **MicrosoftAppId** and **MicrosoftAppPassword** - Teams bot credentials

For detailed information, see:
- [docs/M365-IMPLEMENTATION_SUMMARY.md](docs/M365-IMPLEMENTATION_SUMMARY.md) - Implementation overview
- [docs/M365-CONFIGURATION_GUIDE.md](docs/M365-CONFIGURATION_GUIDE.md) - Configuration details
- [docs/M365-AGENT_INTEGRATION.md](docs/M365-AGENT_INTEGRATION.md) - Agent integration details
- [docs/M365-SETUP_SECRETS.md](docs/M365-SETUP_SECRETS.md) - Secret management

## 3. Blazor WebAssembly App (`ElasticOn.RiskAgent.Demo.Web`)

### Purpose
The Web project provides a modern, responsive chat interface for risk analysis powered by the Azure Functions backend.

### Key Features

#### Chat Interface
- **Real-time Chat** - Interactive chat interface with message history
- **Conversation Management** - State management with conversation and thread IDs
- **Thinking Indicators** - Visual feedback while processing requests
- **Keyboard Support** - Enter to send, Shift+Enter for new lines
- **Responsive Design** - Modern gradient design with purple theme

#### Configuration
- **HttpClient** - Configured to call Azure Functions Chat API
- **Development Mode** - Points to `http://localhost:7071`
- **Production Mode** - Configured for Azure deployment
- **ChatStateService** - Singleton service for state management

For detailed information, see:
- [docs/Web-README.md](docs/Web-README.md) - Web application overview
- [docs/Web-CLARITY_SETUP.md](docs/Web-CLARITY_SETUP.md) - Microsoft Clarity setup
- [docs/Web-LAYOUT_UPDATES.md](docs/Web-LAYOUT_UPDATES.md) - Layout updates

## 4. Tests Project (`ElasticOn.RiskAgent.Demo.Functions.Tests`)

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

## Architecture

The solution uses a microservices architecture with the following components:

```
┌─────────────────────────────────────────────────────────┐
│                    User Interfaces                      │
│  ┌──────────────────┐         ┌────────────────────┐  │
│  │  Blazor WASM App │         │ Microsoft Teams    │  │
│  │  (Web Frontend)  │         │ (M365 Bot)         │  │
│  └────────┬─────────┘         └─────────┬──────────┘  │
└───────────┼───────────────────────────────┼─────────────┘
            │                               │
            ▼                               ▼
┌─────────────────────────────────────────────────────────┐
│              Azure Functions (Backend API)              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ ProcessPdf   │  │ IndexDocument│  │ Chat         │ │
│  │ Function     │  │ Function     │  │ Function     │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────┬────────────────┬────────────────┬────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────┐
│                    External Services                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Elasticsearch│  │ Azure AI     │  │ Azure Content│ │
│  │ (Indexing &  │  │ Foundry      │  │ Safety       │ │
│  │  Search)     │  │ (Agent)      │  │ (Moderation) │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### Key Technologies
- **.NET 9** - Modern C# for all components
- **Azure Functions** - Serverless compute with Flex Consumption plan
- **Azure AI Foundry** - Agent framework with GPT-4 models
- **Elasticsearch** - Document indexing and semantic search
- **Azure Content Safety** - Content moderation and jailbreak detection
- **Microsoft Teams Bot Framework** - Teams integration
- **Blazor WebAssembly** - Modern web UI framework

## Prerequisites

### Development Environment
- **.NET SDK 9** or later
- **Azure Functions Core Tools v4** for running functions locally
- **Visual Studio 2022** or **VS Code** with C# extension
- **Node.js** (for some development tools)

### Azure Services (for deployment)
- **Azure Subscription** with appropriate permissions
- **Azure AI Foundry** project with deployed model
- **Elasticsearch** cluster (cloud or self-hosted)
- **Azure Content Safety** resource (optional, can be disabled)

## Local Development

### Build the Solution

Restore dependencies and build all projects:

```bash
dotnet build ElasticOn.RiskAgent.Demo.sln
```

### Run the Azure Functions (Backend)

```bash
cd src/ElasticOn.RiskAgent.Demo.Functions
func start
```

The Functions API will be available at `http://localhost:7071`

### Run the Blazor Web App

```bash
cd src/ElasticOn.RiskAgent.Demo.Web
dotnet run
```

The web app will be available at `https://localhost:7227` (or similar port)

### Run the Teams Bot

```bash
cd src/ElasticOn.RiskAgent.Demo.M365
dotnet run
```

For Teams integration, see [M365Agent/README.md](M365Agent/README.md) for deployment with Microsoft 365 Agents Toolkit.

### Configuration Files

All projects use configuration files that are excluded from version control. You'll need to create:

- `src/ElasticOn.RiskAgent.Demo.Functions/local.settings.json` - Functions configuration
- `src/ElasticOn.RiskAgent.Demo.M365/appsettings.Development.json` - M365 bot configuration
- `src/ElasticOn.RiskAgent.Demo.Web/wwwroot/appsettings.Development.json` - Web app configuration (if needed)

See the respective project documentation in `/docs` for configuration details.

## Key Features

### PDF Document Processing
- **Base64 decoding** with URL-safe character handling
- **Text extraction** from all PDF pages with multiple extraction methods
- **Intelligent chunking** with configurable size and overlap
- **Metadata processing** with strongly-typed models
- **Automatic indexing** to Elasticsearch with retry logic

### AI-Powered Chat
- **Azure AI Foundry Agent** integration with GPT-4 models
- **Elasticsearch MCP** for retrieving relevant documents
- **Conversation state management** across multiple turns
- **Streaming responses** for real-time interaction
- **Content Safety** with jailbreak detection and moderation

### Enterprise Features
- **Azure Content Safety** - Configurable detection modes (Disabled, Audit, Enforce)
- **Application Insights** - Comprehensive logging and monitoring
- **Managed Identity** - Secure authentication to Azure services
- **Error Handling** - Graceful error messages and retry logic
- **Configurable** - Extensive configuration options for all services

### Multiple Interfaces
- **Azure Functions API** - RESTful API for all operations
- **Microsoft Teams Bot** - Native Teams integration
- **Blazor WebAssembly** - Modern responsive web interface

## Azure Deployment

This project includes Azure Developer CLI (azd) support for easy deployment to Azure.

### Quick Start

1. **Login to Azure**:
   ```bash
   az login
   azd auth login
   ```

2. **Deploy to Azure**: Deploy infrastructure and application
   ```bash
   azd up
   ```

3. **Configure Elasticsearch**: Update your Elasticsearch connection details
   ```bash
   ./scripts/update-elasticsearch-secrets.sh
   ```

### What Gets Deployed

- **Azure Functions App** (Flex Consumption plan) - Backend API
- **Azure OpenAI Service** with text-embedding-ada-002 model - Embeddings for Elasticsearch
- **Application Insights** - Monitoring and logging
- **Storage Account** - Function app storage with managed identity

> **Note**: Azure Key Vault is not currently used due to Flex Consumption plan limitations.  
> Secrets are stored directly in Function App settings.

### Deployment Scripts

- `./scripts/setup-azd-environment.sh` - Verify Azure CLI authentication
- `./scripts/deploy-with-restricted-storage.sh` - Deploy with storage access restrictions
- `./scripts/update-elasticsearch-secrets.sh` - Update Elasticsearch configuration

For complete deployment instructions including Teams bot and web app deployment, see [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md).

## Documentation

Comprehensive documentation is available in the `/docs` directory:

### Getting Started
- [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md) - Complete deployment guide
- [docs/README.md](docs/README.md) - Documentation index

### Function Documentation
- [docs/ChatFunction-README.md](docs/ChatFunction-README.md) - Chat API details
- [docs/ProcessPdfFunction-IndexDocument-README.md](docs/ProcessPdfFunction-IndexDocument-README.md) - PDF processing
- [docs/IndexDocumentFunction-README.md](docs/IndexDocumentFunction-README.md) - Document indexing

### M365 Bot Documentation
- [docs/M365-IMPLEMENTATION_SUMMARY.md](docs/M365-IMPLEMENTATION_SUMMARY.md) - Implementation overview
- [docs/M365-CONFIGURATION_GUIDE.md](docs/M365-CONFIGURATION_GUIDE.md) - Configuration guide
- [docs/M365-AGENT_INTEGRATION.md](docs/M365-AGENT_INTEGRATION.md) - Agent integration

### Web App Documentation
- [docs/Web-README.md](docs/Web-README.md) - Web application overview

### Reference Documentation
- [docs/ContentSafety.md](docs/ContentSafety.md) - Content Safety implementation
- [docs/CUSTOM_ELASTICSEARCH_INDEX.md](docs/CUSTOM_ELASTICSEARCH_INDEX.md) - Custom Elasticsearch configuration
- [docs/Functions-ELASTICSEARCH_CONFIG_EXAMPLES.md](docs/Functions-ELASTICSEARCH_CONFIG_EXAMPLES.md) - Configuration examples

## Next Steps

### For New Users
1. Review the [Architecture](#architecture) section above
2. Follow the [Azure Deployment](#azure-deployment) quick start
3. Configure Elasticsearch and Azure services
4. Test the deployment with provided sample data

### For Developers
1. Build and run the solution locally
2. Review function documentation in `/docs`
3. Explore the test suite for examples
4. Extend with custom features

### Future Enhancements
- Scale the deployment for production workloads
- Implement additional document formats beyond PDF
- Add more sophisticated risk assessment models
- Integrate with additional data sources
- Implement Azure Key Vault when Flex Consumption supports it

## Contributing

This is a demonstration project showcasing Azure AI and Elasticsearch integration. Feel free to use it as a starting point for your own risk assessment solutions.

## License

See [LICENSE](LICENSE) file for details.
