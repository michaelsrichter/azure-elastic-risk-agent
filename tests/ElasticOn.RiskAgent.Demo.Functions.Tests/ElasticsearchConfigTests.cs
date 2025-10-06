using ElasticOn.RiskAgent.Demo.Functions.Models;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

public class ElasticsearchConfigTests
{
    [Fact]
    public void MergeWithFallbacks_WhenAllCustomValuesProvided_UsesCustomValues()
    {
        // Arrange
        var customConfig = new ElasticsearchConfig
        {
            Uri = "http://custom-elasticsearch:9200",
            ApiKey = "custom-api-key",
            IndexName = "custom-index"
        };

        var fallbackUri = "http://fallback:9200";
        var fallbackApiKey = "fallback-key";
        var fallbackIndexName = "fallback-index";

        // Act
        var result = customConfig.MergeWithFallbacks(fallbackUri, fallbackApiKey, fallbackIndexName);

        // Assert
        Assert.Equal("http://custom-elasticsearch:9200", result.Uri);
        Assert.Equal("custom-api-key", result.ApiKey);
        Assert.Equal("custom-index", result.IndexName);
    }

    [Fact]
    public void MergeWithFallbacks_WhenNoCustomValuesProvided_UsesFallbackValues()
    {
        // Arrange
        var customConfig = new ElasticsearchConfig
        {
            Uri = null,
            ApiKey = null,
            IndexName = null
        };

        var fallbackUri = "http://fallback:9200";
        var fallbackApiKey = "fallback-key";
        var fallbackIndexName = "fallback-index";

        // Act
        var result = customConfig.MergeWithFallbacks(fallbackUri, fallbackApiKey, fallbackIndexName);

        // Assert
        Assert.Equal(fallbackUri, result.Uri);
        Assert.Equal(fallbackApiKey, result.ApiKey);
        Assert.Equal(fallbackIndexName, result.IndexName);
    }

    [Fact]
    public void MergeWithFallbacks_WhenEmptyCustomValuesProvided_UsesFallbackValues()
    {
        // Arrange
        var customConfig = new ElasticsearchConfig
        {
            Uri = "",
            ApiKey = "   ",
            IndexName = ""
        };

        var fallbackUri = "http://fallback:9200";
        var fallbackApiKey = "fallback-key";
        var fallbackIndexName = "fallback-index";

        // Act
        var result = customConfig.MergeWithFallbacks(fallbackUri, fallbackApiKey, fallbackIndexName);

        // Assert
        Assert.Equal(fallbackUri, result.Uri);
        Assert.Equal(fallbackApiKey, result.ApiKey);
        Assert.Equal(fallbackIndexName, result.IndexName);
    }

    [Fact]
    public void MergeWithFallbacks_WhenPartialCustomValuesProvided_UsesMixOfCustomAndFallback()
    {
        // Arrange
        var customConfig = new ElasticsearchConfig
        {
            Uri = "http://custom-elasticsearch:9200",
            ApiKey = null,
            IndexName = "custom-index"
        };

        var fallbackUri = "http://fallback:9200";
        var fallbackApiKey = "fallback-key";
        var fallbackIndexName = "fallback-index";

        // Act
        var result = customConfig.MergeWithFallbacks(fallbackUri, fallbackApiKey, fallbackIndexName);

        // Assert
        Assert.Equal("http://custom-elasticsearch:9200", result.Uri);
        Assert.Equal("fallback-key", result.ApiKey);
        Assert.Equal("custom-index", result.IndexName);
    }

    [Fact]
    public void MergeWithFallbacks_WhenFallbackApiKeyIsNull_HandlesGracefully()
    {
        // Arrange
        var customConfig = new ElasticsearchConfig
        {
            Uri = "http://custom:9200",
            ApiKey = null,
            IndexName = "custom-index"
        };

        var fallbackUri = "http://fallback:9200";
        string? fallbackApiKey = null;
        var fallbackIndexName = "fallback-index";

        // Act
        var result = customConfig.MergeWithFallbacks(fallbackUri, fallbackApiKey, fallbackIndexName);

        // Assert
        Assert.Equal("http://custom:9200", result.Uri);
        Assert.Null(result.ApiKey);
        Assert.Equal("custom-index", result.IndexName);
    }
}