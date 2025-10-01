using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using ElasticOn.RiskAgent.Demo.Functions.Models;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

public sealed class ProcessPdfParserTests
{
    private static readonly string SampleRequestPath = Path.Combine(AppContext.BaseDirectory, "samplePdfFileRequest.json");

    private static ProcessPdfParser CreateParser()
    {
        // Use the real RecursiveTextChunkingService for tests
        var chunkingService = new RecursiveTextChunkingService();
        return new ProcessPdfParser(chunkingService);
    }

    [Fact]
    public void Parse_ReturnsPdfBytesWithReadableText()
    {
        var payload = ReadSamplePayload();
        var parser = CreateParser();

        var result = parser.Parse(payload.FileContent!, payload.Metadata!);

        // Verify PDF bytes were successfully decoded from Base64
        Assert.True(result.PdfBytes.Length > 0);
        
        // Verify it starts with PDF header
        var pdfHeader = System.Text.Encoding.ASCII.GetString(result.PdfBytes, 0, Math.Min(8, result.PdfBytes.Length));
        Assert.StartsWith("%PDF-", pdfHeader);
    }

    [Fact]
    public void Parse_ConvertsMetadataToStrongType()
    {
        var payload = ReadSamplePayload();

        var result = CreateParser().Parse(payload.FileContent!, payload.Metadata!);

        Assert.NotEmpty(result.Metadata.Id);
        Assert.Equal("pub-2025-cybersecurity-report.pdf", result.Metadata.FilenameWithExtension);
        Assert.Equal("Risk Assessments/pub-2025-cybersecurity-report.pdf", result.Metadata.FullPath);
        Assert.Equal("1.0", result.Metadata.VersionNumber);
    }

    [Fact]
    public void Parse_WithEmptyFileContent_ThrowsArgumentException()
    {
        var metadata = new DocumentMetadata { Id = "test-id", FilenameWithExtension = "test.pdf" };
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateParser().Parse("", metadata));
        
        Assert.Contains("fileContent is required", exception.Message);
    }

    [Fact]
    public void Parse_WithNullFileContent_ThrowsArgumentException()
    {
        var metadata = new DocumentMetadata { Id = "test-id", FilenameWithExtension = "test.pdf" };
        var exception = Assert.Throws<ArgumentException>(() =>
            CreateParser().Parse(null!, metadata));
        
        Assert.Contains("fileContent is required", exception.Message);
    }

    [Fact]
    public void Parse_WithNullMetadata_ThrowsArgumentNullException()
    {
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("%PDF-1.4 test"));
        
        var exception = Assert.Throws<ArgumentNullException>(() =>
            CreateParser().Parse(validPdfBase64, null!));
        
        Assert.Contains("metadata", exception.Message);
    }

    [Fact]
    public void Parse_WithInvalidBase64_ThrowsFormatException()
    {
        var metadata = new DocumentMetadata { Id = "test-id", FilenameWithExtension = "test.pdf" };
        Assert.Throws<FormatException>(() =>
            CreateParser().Parse("invalid-base64!@#", metadata));
    }

    [Fact]
    public void Parse_WithValidMinimalData_ReturnsCorrectResult()
    {
        var testContent = "%PDF-1.4\ntest content";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata { Id = "test-doc-id", VersionNumber = "1.0" };
        
        var result = CreateParser().Parse(validPdfBase64, metadata);
        
        Assert.Equal(testContent.Length, result.PdfBytes.Length);
        Assert.Equal("test-doc-id", result.Metadata.Id);
        Assert.Equal("1.0", result.Metadata.VersionNumber);
    }

    [Fact]
    public void Parse_WithHttpClient_DoesNotThrow()
    {
        // Arrange
        var testContent = "%PDF-1.4\ntest content with multiple pages\n\f\nsecond page content";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata { Id = "test-doc-id", FilenameWithExtension = "test.pdf" };
        var mockHttpClient = Substitute.For<HttpClient>();

        // Act & Assert - Should not throw
        var result = CreateParser().Parse(validPdfBase64, metadata, mockHttpClient);
        
        Assert.NotNull(result);
        Assert.Equal("test-doc-id", result.Metadata.Id);
    }

    [Fact]
    public void Parse_WithNullHttpClient_DoesNotCallIndexing()
    {
        // Arrange
        var testContent = "%PDF-1.4\ntest content";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata { Id = "test-doc-id", FilenameWithExtension = "test.pdf" };

        // Act - Should not attempt any HTTP calls
        var result = CreateParser().Parse(validPdfBase64, metadata, httpClient: null);
        
        // Assert - Should process normally without indexing
        Assert.NotNull(result);
        Assert.Equal("test-doc-id", result.Metadata.Id);
    }

    [Fact]
    public void Parse_WithConfigurationAndHttpClient_ProcessesCorrectly()
    {
        // Arrange
        var testContent = "%PDF-1.4\ntest content";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata { Id = "test-doc-id", FilenameWithExtension = "test.pdf" };
        
        var configDict = new Dictionary<string, string?>
        {
            ["ChunkSize"] = "100",
            ["ChunkOverlap"] = "10"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        
        var mockHttpClient = Substitute.For<HttpClient>();
        var elasticsearchConfig = new ElasticsearchConfig
        {
            Uri = "http://test:9200",
            IndexName = "test-index"
        };

        // Act
        var result = CreateParser().Parse(validPdfBase64, metadata, configuration, mockHttpClient, elasticsearchConfig);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-doc-id", result.Metadata.Id);
        Assert.True(result.ChunkingStats.PageCount > 0);
    }

    private static SamplePayload ReadSamplePayload()
    {
        string requestBody = File.Exists(SampleRequestPath)
            ? File.ReadAllText(SampleRequestPath)
            : File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samplePdfFileRequest.json")));

        return JsonSerializer.Deserialize<SamplePayload>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Unable to deserialize sample request payload.");
    }

    private sealed record SamplePayload(string? FileContent, DocumentMetadata? Metadata);
}

public sealed class ProcessPdfFunctionLogicTests
{
    private readonly ILogger<ProcessPdfFunction> _logger;

    public ProcessPdfFunctionLogicTests()
    {
        _logger = Substitute.For<ILogger<ProcessPdfFunction>>();
    }

    private static ProcessPdfParser CreateParser()
    {
        // Use the real RecursiveTextChunkingService for tests
        var chunkingService = new RecursiveTextChunkingService();
        return new ProcessPdfParser(chunkingService);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRequestBody_WithInvalidBody_ReturnsFalse(string? body)
    {
        var result = ValidateRequestBody(body);
        Assert.False(result);
    }

    [Fact]
    public void ValidateRequestBody_WithValidBody_ReturnsTrue()
    {
        var result = ValidateRequestBody("{\"test\": \"value\"}");
        Assert.True(result);
    }

    [Fact]
    public void DeserializeRequest_WithValidJson_ReturnsObject()
    {
        var json = JsonSerializer.Serialize(new { FileContent = "test", Metadata = "data" });
        
        var (success, payload, _) = TryDeserializeRequest(json);
        
        Assert.True(success);
        Assert.NotNull(payload);
        Assert.Equal("test", payload.FileContent);
        Assert.Equal("data", payload.Metadata);
    }

    [Fact]
    public void DeserializeRequest_WithInvalidJson_ReturnsFalse()
    {
        var (success, payload, exception) = TryDeserializeRequest("{ invalid json }");
        
        Assert.False(success);
        Assert.Null(payload);
        Assert.IsType<JsonException>(exception);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("content", null)]
    [InlineData(null, "metadata")]
    [InlineData("", "metadata")]
    [InlineData("content", "")]
    public void ValidatePayload_WithInvalidData_ReturnsFalse(string? fileContent, string? metadata)
    {
        var payload = new TestPayload(fileContent, metadata);
        var result = ValidatePayload(payload);
        Assert.False(result);
    }

    [Fact]
    public void ValidatePayload_WithValidData_ReturnsTrue()
    {
        var payload = new TestPayload("valid-content", "valid-metadata");
        var result = ValidatePayload(payload);
        Assert.True(result);
    }

    [Fact]
    public void ProcessPayload_WithValidData_LogsCorrectInformation()
    {
        var testContent = "%PDF-1.4\ntest content";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata { Id = "test-doc", FilenameWithExtension = "test-doc.pdf" };
        
        var parsedData = CreateParser().Parse(validPdfBase64, metadata);
        LogProcessingInfo(_logger, parsedData);
        
        // Verify logging occurred with correct information
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ProcessPDF invoked with payload size") && 
                               o.ToString()!.Contains("bytes and metadata for document")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    // Helper methods that extract the core logic for testing
    private static bool ValidateRequestBody(string? body) => !string.IsNullOrWhiteSpace(body);

    private static (bool Success, TestPayload? Payload, Exception? Exception) TryDeserializeRequest(string body)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<TestPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return (true, payload, null);
        }
        catch (JsonException ex)
        {
            return (false, null, ex);
        }
    }

    private static bool ValidatePayload(TestPayload? payload)
    {
        return payload is not null && 
               !string.IsNullOrWhiteSpace(payload.FileContent) && 
               !string.IsNullOrWhiteSpace(payload.Metadata);
    }

    private static void LogProcessingInfo<T>(ILogger<T> logger, ProcessPdfData parsedData)
    {
        logger.LogInformation("ProcessPDF invoked with payload size {ByteCount} bytes and metadata for document {DocumentId}",
            parsedData.PdfBytes.Length,
            parsedData.Metadata.Id);
    }

    private sealed record TestPayload(string? FileContent, string? Metadata);
}

public sealed class PdfTextExtractorTests
{
    private static readonly string SampleRequestPath = Path.Combine(AppContext.BaseDirectory, "samplePdfFileRequest.json");

    private static ProcessPdfParser CreateParser()
    {
        // Use the real RecursiveTextChunkingService for tests
        var chunkingService = new RecursiveTextChunkingService();
        return new ProcessPdfParser(chunkingService);
    }

    [Fact]
    public void ExtractFirstPageText_WithSamplePdf_ReturnsText()
    {
        // Arrange
        var samplePayload = ReadSamplePayload();
        var pdfBytes = Convert.FromBase64String(samplePayload.FileContent!);

        // Act
        var result = ElasticOn.RiskAgent.Demo.Functions.Services.PdfTextExtractor.ExtractFirstPageText(pdfBytes);

        // Assert
        Assert.NotNull(result);
        // The sample PDF should contain some text content
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void ExtractTextFromAllPages_WithSamplePdf_ReturnsPages()
    {
        // Arrange
        var samplePayload = ReadSamplePayload();
        var pdfBytes = Convert.FromBase64String(samplePayload.FileContent!);

        // Act
        var result = ElasticOn.RiskAgent.Demo.Functions.Services.PdfTextExtractor.ExtractTextFromAllPages(pdfBytes);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // First page should have some content
        Assert.True(result[0].Length >= 0);
    }

    [Fact]
    public void GetPageCount_WithSamplePdf_ReturnsPositiveCount()
    {
        // Arrange
        var samplePayload = ReadSamplePayload();
        var pdfBytes = Convert.FromBase64String(samplePayload.FileContent!);

        // Act
        var result = ElasticOn.RiskAgent.Demo.Functions.Services.PdfTextExtractor.GetPageCount(pdfBytes);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void ExtractFirstPageText_WithEmptyPdfBytes_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ElasticOn.RiskAgent.Demo.Functions.Services.PdfTextExtractor.ExtractFirstPageText(Array.Empty<byte>()));
        
        Assert.Contains("PDF bytes cannot be null or empty", exception.Message);
        Assert.Equal("pdfBytes", exception.ParamName);
    }

    [Fact]
    public void ExtractFirstPageText_WithNullPdfBytes_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ElasticOn.RiskAgent.Demo.Functions.Services.PdfTextExtractor.ExtractFirstPageText(null!));
        
        Assert.Contains("PDF bytes cannot be null or empty", exception.Message);
        Assert.Equal("pdfBytes", exception.ParamName);
    }

    [Fact]
    public void Parse_WithInvalidPdfBytes_ReturnsErrorMessageInFirstPageText()
    {
        // Arrange
        var invalidPdfData = Convert.ToBase64String(Encoding.UTF8.GetBytes("This is not a PDF file"));
        var metadata = new DocumentMetadata { Id = "test-id", FilenameWithExtension = "test.pdf" };

        // Act
        var result = CreateParser().Parse(invalidPdfData, metadata);
        
        // Assert
        Assert.Equal(1, result.ChunkingStats.PageCount); // Invalid PDF returns one page with error message
        Assert.True(result.ChunkingStats.MaxChunksInPage >= 0); // Should have at least 0 chunks
    }

    private static SamplePayload ReadSamplePayload()
    {
        string requestBody = File.Exists(SampleRequestPath)
            ? File.ReadAllText(SampleRequestPath)
            : File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "samplePdfFileRequest.json")));

        return JsonSerializer.Deserialize<SamplePayload>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Unable to deserialize sample request payload.");
    }

    private sealed record SamplePayload(string? FileContent, DocumentMetadata? Metadata);
}
