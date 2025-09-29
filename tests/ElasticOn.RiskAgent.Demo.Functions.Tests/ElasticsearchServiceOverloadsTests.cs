using ElasticOn.RiskAgent.Demo.Functions.Models;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

public class ElasticsearchServiceOverloadsTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ElasticsearchService>> _mockLogger;

    public ElasticsearchServiceOverloadsTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ElasticsearchService>>();

        // Setup default configuration values
        _mockConfiguration.Setup(x => x["ElasticsearchUri"]).Returns("http://localhost:9200");
        _mockConfiguration.Setup(x => x["ElasticsearchApiKey"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["ElasticsearchIndexName"]).Returns("risk-agent-documents");
    }

    [Fact]
    public void ElasticsearchService_ConstructorWithConfiguration_InitializesCorrectly()
    {
        // Act
        var service = new ElasticsearchService(_mockConfiguration.Object, _mockLogger.Object);

        // Assert - Service should be created without throwing
        Assert.NotNull(service);
    }

    [Fact]
    public void ElasticsearchService_HasIndexDocumentOverloads_BothMethodsAvailable()
    {
        // Arrange
        var service = new ElasticsearchService(_mockConfiguration.Object, _mockLogger.Object);
        var document = new ElasticsearchDocument
        {
            Id = "test-id",
            FilenameWithExtension = "test.pdf",
            PageNumber = 1,
            PageChunkNumber = 1,
            Chunk = "Test content"
        };

        // Act & Assert - Verify both overloads exist by checking method signatures
        var methods = typeof(IElasticsearchService).GetMethods()
            .Where(m => m.Name == "IndexDocumentAsync")
            .ToArray();

        Assert.Equal(2, methods.Length);
        
        // Check default method signature
        var defaultMethod = methods.First(m => m.GetParameters().Length == 2);
        Assert.Equal(typeof(Task<bool>), defaultMethod.ReturnType);
        Assert.Equal(typeof(ElasticsearchDocument), defaultMethod.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), defaultMethod.GetParameters()[1].ParameterType);

        // Check custom config method signature
        var customConfigMethod = methods.First(m => m.GetParameters().Length == 3);
        Assert.Equal(typeof(Task<bool>), customConfigMethod.ReturnType);
        Assert.Equal(typeof(ElasticsearchDocument), customConfigMethod.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(ElasticsearchConfig), customConfigMethod.GetParameters()[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), customConfigMethod.GetParameters()[2].ParameterType);
    }

    [Fact]
    public void ElasticsearchService_HasEnsureIndexExistsOverloads_BothMethodsAvailable()
    {
        // Arrange
        var service = new ElasticsearchService(_mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert - Verify both overloads exist by checking method signatures
        var methods = typeof(IElasticsearchService).GetMethods()
            .Where(m => m.Name == "EnsureIndexExistsAsync")
            .ToArray();

        Assert.Equal(2, methods.Length);
        
        // Check default method signature
        var defaultMethod = methods.First(m => m.GetParameters().Length == 1);
        Assert.Equal(typeof(Task<bool>), defaultMethod.ReturnType);
        Assert.Equal(typeof(CancellationToken), defaultMethod.GetParameters()[0].ParameterType);

        // Check custom config method signature
        var customConfigMethod = methods.First(m => m.GetParameters().Length == 2);
        Assert.Equal(typeof(Task<bool>), customConfigMethod.ReturnType);
        Assert.Equal(typeof(ElasticsearchConfig), customConfigMethod.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), customConfigMethod.GetParameters()[1].ParameterType);
    }

    [Fact]
    public void ElasticsearchService_WithCustomConfigurationValues_MergesCorrectly()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["ElasticsearchUri"]).Returns("http://default:9200");
        _mockConfiguration.Setup(x => x["ElasticsearchApiKey"]).Returns("default-key");
        _mockConfiguration.Setup(x => x["ElasticsearchIndexName"]).Returns("default-index");

        var service = new ElasticsearchService(_mockConfiguration.Object, _mockLogger.Object);
        
        var customConfig = new ElasticsearchConfig
        {
            Uri = "http://custom:9200",
            ApiKey = null, // Should fallback to default
            IndexName = "custom-index"
        };

        var merged = customConfig.MergeWithFallbacks("http://default:9200", "default-key", "default-index");

        // Assert
        Assert.Equal("http://custom:9200", merged.Uri);
        Assert.Equal("default-key", merged.ApiKey);
        Assert.Equal("custom-index", merged.IndexName);
    }

    [Fact]
    public void ElasticsearchService_WithNullApiKey_HandlesGracefully()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["ElasticsearchUri"]).Returns("http://localhost:9200");
        _mockConfiguration.Setup(x => x["ElasticsearchApiKey"]).Returns((string?)null);
        _mockConfiguration.Setup(x => x["ElasticsearchIndexName"]).Returns("test-index");

        // Act & Assert - Should not throw
        var service = new ElasticsearchService(_mockConfiguration.Object, _mockLogger.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public void ElasticsearchService_WithEmptyApiKey_HandlesGracefully()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["ElasticsearchUri"]).Returns("http://localhost:9200");
        _mockConfiguration.Setup(x => x["ElasticsearchApiKey"]).Returns("");
        _mockConfiguration.Setup(x => x["ElasticsearchIndexName"]).Returns("test-index");

        // Act & Assert - Should not throw
        var service = new ElasticsearchService(_mockConfiguration.Object, _mockLogger.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public void ElasticsearchConfig_MergeWithFallbacks_PreservesNonNullCustomValues()
    {
        // Arrange
        var customConfig = new ElasticsearchConfig
        {
            Uri = "http://custom:9200",
            ApiKey = "custom-key",
            IndexName = "custom-index"
        };

        // Act
        var merged = customConfig.MergeWithFallbacks("http://fallback:9200", "fallback-key", "fallback-index");

        // Assert - All custom values should be preserved
        Assert.Equal("http://custom:9200", merged.Uri);
        Assert.Equal("custom-key", merged.ApiKey);
        Assert.Equal("custom-index", merged.IndexName);
    }

    [Fact]
    public void ElasticsearchConfig_MergeWithFallbacks_UsesFallbacksForNullValues()
    {
        // Arrange
        var customConfig = new ElasticsearchConfig
        {
            Uri = null,
            ApiKey = null,
            IndexName = null
        };

        // Act
        var merged = customConfig.MergeWithFallbacks("http://fallback:9200", "fallback-key", "fallback-index");

        // Assert - All fallback values should be used
        Assert.Equal("http://fallback:9200", merged.Uri);
        Assert.Equal("fallback-key", merged.ApiKey);
        Assert.Equal("fallback-index", merged.IndexName);
    }
}