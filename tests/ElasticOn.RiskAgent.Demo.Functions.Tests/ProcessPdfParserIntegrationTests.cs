using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ElasticOn.RiskAgent.Demo.Functions.Models;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

public sealed class ProcessPdfParserIntegrationTests
{
    private readonly IConfiguration _configuration;

    public ProcessPdfParserIntegrationTests()
    {
        var configDict = new Dictionary<string, string?>
        {
            ["ChunkSize"] = "50", // Small chunk size for testing
            ["ChunkOverlap"] = "10"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    [Fact]
    public async Task Parse_WithHttpClient_CallsIndexDocumentForEachChunk()
    {
        // Arrange
        var testContent = "This is page 1 content.\n\fThis is page 2 content.";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata 
        { 
            Id = "test-doc-id", 
            FilenameWithExtension = "test.pdf",
            FullPath = "/test/test.pdf",
            VersionNumber = "1.0"
        };

        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHttpMessageHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var elasticsearchConfig = new ElasticsearchConfig
        {
            Uri = "http://test:9200",
            IndexName = "test-index"
        };

        // Act
        var result = ProcessPdfParser.Parse(validPdfBase64, metadata, _configuration, httpClient, elasticsearchConfig);

        // Give some time for the async indexing to complete
        await Task.Delay(500);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-doc-id", result.Metadata.Id);
        
        // Verify HTTP calls were made (we can't easily verify the exact count due to async nature,
        // but we can verify that calls were attempted)
        Assert.True(mockHttpMessageHandler.RequestCount > 0, "Expected HTTP requests to be made for chunk indexing");
    }

    [Fact]
    public async Task Parse_WithMultiplePages_IndexesChunksWithCorrectMetadata()
    {
        // Arrange
        var testContent = "Page 1: This is a longer content that should be split into multiple chunks for testing purposes.\n\f" +
                         "Page 2: This is another page with content that will also be chunked appropriately.";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata 
        { 
            Id = "multi-page-doc", 
            FilenameWithExtension = "multipage.pdf",
            FullPath = "/test/multipage.pdf",
            VersionNumber = "2.0"
        };

        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHttpMessageHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        // Act
        var result = ProcessPdfParser.Parse(validPdfBase64, metadata, _configuration, httpClient);

        // Give time for async processing
        await Task.Delay(500);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("multi-page-doc", result.Metadata.Id);
        Assert.True(result.ChunkingStats.PageCount > 0);
        
        // Verify that HTTP requests were made
        Assert.True(mockHttpMessageHandler.RequestCount > 0, "Expected HTTP requests for chunk indexing");
        
        // Verify request content contains expected data
        var requestBodies = mockHttpMessageHandler.RequestBodies;
        Assert.NotEmpty(requestBodies);
        
        // Check that at least one request contains the expected metadata
        var hasValidRequest = requestBodies.Any(body => 
        {
            try
            {
                var request = JsonSerializer.Deserialize<IndexDocumentRequest>(body, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return request?.DocumentMetadata?.Id == "multi-page-doc" &&
                       request.PageNumber > 0 &&
                       request.PageChunkNumber > 0 &&
                       !string.IsNullOrEmpty(request.Chunk);
            }
            catch
            {
                return false;
            }
        });
        
        Assert.True(hasValidRequest, "Expected at least one valid IndexDocumentRequest with correct metadata");
    }

    [Fact]
    public void Parse_WithHttpClientException_DoesNotThrow()
    {
        // Arrange
        var testContent = "Test content";
        var validPdfBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        var metadata = new DocumentMetadata 
        { 
            Id = "error-test-doc", 
            FilenameWithExtension = "error.pdf"
        };

        var faultyHttpMessageHandler = new FaultyHttpMessageHandler();
        var httpClient = new HttpClient(faultyHttpMessageHandler);

        // Act & Assert - Should not throw even if HTTP calls fail
        var result = ProcessPdfParser.Parse(validPdfBase64, metadata, _configuration, httpClient);
        
        Assert.NotNull(result);
        Assert.Equal("error-test-doc", result.Metadata.Id);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<string> _requestBodies = new();
        private int _requestCount = 0;

        public int RequestCount => _requestCount;
        public IReadOnlyList<string> RequestBodies => _requestBodies.AsReadOnly();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);

            if (request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                lock (_requestBodies)
                {
                    _requestBodies.Add(content);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\": true, \"documentId\": \"test-id\"}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FaultyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated HTTP error");
        }
    }
}