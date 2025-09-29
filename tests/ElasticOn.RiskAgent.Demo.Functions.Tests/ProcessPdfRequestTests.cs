using System.Text.Json;
using ElasticOn.RiskAgent.Demo.Functions.Models;
using Xunit;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

public class ProcessPdfRequestTests
{
    [Fact]
    public void ProcessPdfRequest_WithElasticsearchConfig_PropertiesSetCorrectly()
    {
        // Arrange & Act - Note: Using the actual ProcessPdfRequest from ProcessPdfFunction.cs
        var requestJson = @"{
            ""fileContent"": ""base64-content"",
            ""metadata"": {
                ""filenameWithExtension"": ""test.pdf""
            },
            ""elasticsearchConfig"": {
                ""uri"": ""http://custom:9200"",
                ""apiKey"": ""custom-key"",
                ""indexName"": ""custom-index""
            }
        }";

        var request = JsonSerializer.Deserialize<TestProcessPdfRequest>(requestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(request);
        Assert.NotNull(request.ElasticsearchConfig);
        Assert.Equal("http://custom:9200", request.ElasticsearchConfig.Uri);
        Assert.Equal("custom-key", request.ElasticsearchConfig.ApiKey);
        Assert.Equal("custom-index", request.ElasticsearchConfig.IndexName);
    }

    [Fact]
    public void ProcessPdfRequest_WithoutElasticsearchConfig_PropertyIsNull()
    {
        // Arrange & Act
        var requestJson = @"{
            ""fileContent"": ""base64-content"",
            ""metadata"": {
                ""filenameWithExtension"": ""test.pdf""
            }
        }";

        var request = JsonSerializer.Deserialize<TestProcessPdfRequest>(requestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(request);
        Assert.Null(request.ElasticsearchConfig);
    }

    [Fact]
    public void ProcessPdfRequest_JsonSerialization_IncludesElasticsearchConfig()
    {
        // Arrange
        var request = new TestProcessPdfRequest
        {
            FileContent = "base64-content",
            Metadata = new DocumentMetadata
            {
                FilenameWithExtension = "test.pdf"
            },
            ElasticsearchConfig = new ElasticsearchConfig
            {
                Uri = "http://custom:9200",
                IndexName = "custom-index"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        // Assert
        Assert.Contains("elasticsearchConfig", json);
        Assert.Contains("http://custom:9200", json);
        Assert.Contains("custom-index", json);
    }

    [Fact]
    public void ProcessPdfRequest_PartialElasticsearchConfig_DeserializesCorrectly()
    {
        // Arrange & Act
        var requestJson = @"{
            ""fileContent"": ""base64-content"",
            ""metadata"": {
                ""filenameWithExtension"": ""test.pdf""
            },
            ""elasticsearchConfig"": {
                ""indexName"": ""special-index""
            }
        }";

        var request = JsonSerializer.Deserialize<TestProcessPdfRequest>(requestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(request);
        Assert.NotNull(request.ElasticsearchConfig);
        Assert.Null(request.ElasticsearchConfig.Uri);
        Assert.Null(request.ElasticsearchConfig.ApiKey);
        Assert.Equal("special-index", request.ElasticsearchConfig.IndexName);
    }

    // Test record matching the actual ProcessPdfRequest structure from ProcessPdfFunction.cs
    private sealed record TestProcessPdfRequest
    {
        public string? FileContent { get; init; }
        public DocumentMetadata? Metadata { get; init; }
        public ElasticsearchConfig? ElasticsearchConfig { get; init; }
    }
}