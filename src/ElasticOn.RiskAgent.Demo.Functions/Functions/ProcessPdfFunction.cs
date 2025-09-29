using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using ElasticOn.RiskAgent.Demo.Functions.Models;

namespace ElasticOn.RiskAgent.Demo.Functions;

public sealed class ProcessPdfFunction
{
    private readonly ILogger<ProcessPdfFunction> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProcessPdfFunction(ILogger<ProcessPdfFunction> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [Function("ProcessPDF")]
    [OpenApiOperation(operationId: "ProcessPDF", tags: new[] { "Documents" }, Summary = "Process PDF document", Description = "Processes a PDF document to extract text content and metadata")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(ProcessPdfRequest), Description = "JSON request body containing base64 encoded PDF content and metadata")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "PDF processed successfully with extracted content")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Bad request - invalid or missing request data")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error occurred while processing")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "process-pdf")] HttpRequestData request)
    {
        string? body = await request.ReadAsStringAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Request body is empty.")
                .ConfigureAwait(false);
        }

        ProcessPdfRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ProcessPdfRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize request body into ProcessPdfRequest.");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Invalid JSON payload.")
                .ConfigureAwait(false);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.FileContent) || payload.Metadata is null)
        {
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest,
                    "Both fileContent and metadata are required.")
                .ConfigureAwait(false);
        }

        ProcessPdfData parsedData;
        try
        {
            // Use HttpClientFactory for indexing if indexDocument is true
            var httpClientFactory = payload.IndexDocument ? _httpClientFactory : null;
            parsedData = ProcessPdfParser.Parse(payload.FileContent, payload.Metadata, _configuration, httpClientFactory, payload.ElasticsearchConfig);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "fileContent must be a valid Base64 string.");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest,
                    "fileContent must be a valid Base64 string.")
                .ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid payload supplied to ProcessPDF.");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, ex.Message)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("ProcessPDF invoked with payload size {ByteCount} bytes and metadata for document {DocumentId}. Indexing: {IndexingEnabled}",
            parsedData.PdfBytes.Length,
            parsedData.Metadata.Id,
            payload.IndexDocument);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            message = payload.IndexDocument ? "PDF accepted for processing and indexing." : "PDF accepted for processing.",
            size = parsedData.PdfBytes.Length,
            metadata = parsedData.Metadata,
            indexingEnabled = payload.IndexDocument,
            elasticsearchConfig = payload.ElasticsearchConfig != null ? new
            {
                hasCustomConfig = true,
                indexName = payload.ElasticsearchConfig.IndexName,
                uri = payload.ElasticsearchConfig.Uri
            } : new { hasCustomConfig = false, indexName = (string?)null, uri = (string?)null },
            document = new
            {
                parsedData.Metadata.Id,
                parsedData.Metadata.FilenameWithExtension,
                parsedData.Metadata.VersionNumber,
                parsedData.ChunkingStats.PageCount,
                parsedData.ChunkingStats.AverageChunksPerPage,
                parsedData.ChunkingStats.MaxChunksPerPage,
                parsedData.ChunkingStats.MinChunksPerPage
            }
        }).ConfigureAwait(false);

        return response;
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string errorMessage)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = errorMessage }).ConfigureAwait(false);
        return response;
    }

    private sealed record ProcessPdfRequest
    {
        [JsonPropertyName("fileContent")]
        public string? FileContent { get; init; }

        [JsonPropertyName("metadata")]
        public DocumentMetadata? Metadata { get; init; }

        [JsonPropertyName("elasticsearchConfig")]
        public ElasticsearchConfig? ElasticsearchConfig { get; init; }

        [JsonPropertyName("indexDocument")]
        public bool IndexDocument { get; init; } = false;
    }
}
