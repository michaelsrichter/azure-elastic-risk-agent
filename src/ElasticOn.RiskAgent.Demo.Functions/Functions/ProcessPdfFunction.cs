using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using ElasticOn.RiskAgent.Demo.Functions.Models;

namespace ElasticOn.RiskAgent.Demo.Functions;

public sealed class ProcessPdfFunction
{
    private readonly ILogger<ProcessPdfFunction> _logger;

    public ProcessPdfFunction(ILogger<ProcessPdfFunction> logger)
    {
        _logger = logger;
    }

    [Function("ProcessPDF")]
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
            parsedData = ProcessPdfParser.Parse(payload.FileContent, payload.Metadata);
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

        _logger.LogInformation("ProcessPDF invoked with payload size {ByteCount} bytes and metadata for document {DocumentId}",
            parsedData.PdfBytes.Length,
            parsedData.Metadata.Id);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            message = "PDF accepted for processing.",
            size = parsedData.PdfBytes.Length,
            metadata = parsedData.Metadata,
            document = new
            {
                parsedData.Metadata.Id,
                parsedData.Metadata.FilenameWithExtension,
                parsedData.Metadata.VersionNumber,
                PageCount = parsedData.PageCount,
                FirstPageText = parsedData.FirstPageText
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

    private sealed record ProcessPdfRequest(string? FileContent, DocumentMetadata? Metadata);
}
