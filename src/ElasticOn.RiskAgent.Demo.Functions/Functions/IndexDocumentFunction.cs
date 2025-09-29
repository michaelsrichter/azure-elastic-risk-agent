using System.Net;
using System.Text.Json;
using ElasticOn.RiskAgent.Demo.Functions.Models;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ElasticOn.RiskAgent.Demo.Functions.Functions;

internal sealed class IndexDocumentFunction
{
    private readonly ILogger<IndexDocumentFunction> _logger;
    private readonly IElasticsearchService _elasticsearchService;

    public IndexDocumentFunction(ILogger<IndexDocumentFunction> logger, IElasticsearchService elasticsearchService)
    {
        _logger = logger;
        _elasticsearchService = elasticsearchService;
    }

    [Function("IndexDocument")]
    [OpenApiOperation(operationId: "IndexDocument", tags: new[] { "Documents" }, Summary = "Index a document chunk", Description = "Indexes a document chunk with metadata into Elasticsearch")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody("application/json", typeof(IndexDocumentRequest), Description = "JSON request body containing document chunk data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "Document successfully indexed with document ID")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Bad request - invalid or missing request data")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Description = "Internal server error occurred while processing")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "index-document")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("IndexDocument function processing a request.");

        try
        {
            // Read and deserialize the request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogWarning("Request body is empty");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body cannot be empty", cancellationToken);
                return badRequestResponse;
            }

            var indexRequest = JsonSerializer.Deserialize<IndexDocumentRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (indexRequest?.DocumentMetadata == null)
            {
                _logger.LogWarning("Invalid request: DocumentMetadata is required");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("DocumentMetadata is required", cancellationToken);
                return badRequestResponse;
            }

            if (string.IsNullOrEmpty(indexRequest.DocumentMetadata.FilenameWithExtension))
            {
                _logger.LogWarning("Invalid request: FilenameWithExtension is required");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("FilenameWithExtension is required", cancellationToken);
                return badRequestResponse;
            }

            // Convert to Elasticsearch document
            var elasticsearchDocument = ElasticsearchDocument.FromRequest(indexRequest);

            _logger.LogInformation("Processing document with generated ID: {DocumentId}", elasticsearchDocument.Id);

            // Index the document using custom config if provided, otherwise use default config
            var success = await _elasticsearchService.IndexDocumentAsync(elasticsearchDocument, indexRequest.ElasticsearchConfig, cancellationToken);

            if (success)
            {
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                var responseData = new
                {
                    success = true,
                    documentId = elasticsearchDocument.Id,
                    message = "Document successfully indexed"
                };
                await successResponse.WriteStringAsync(JsonSerializer.Serialize(responseData), cancellationToken);
                return successResponse;
            }
            else
            {
                _logger.LogError("Failed to index document");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to index document", cancellationToken);
                return errorResponse;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in request body");
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("Invalid JSON format", cancellationToken);
            return badRequestResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the request");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An internal error occurred", cancellationToken);
            return errorResponse;
        }
    }
}