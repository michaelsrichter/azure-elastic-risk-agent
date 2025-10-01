using System.Text.Json.Serialization;

namespace ElasticOn.RiskAgent.Demo.Functions.Models;

/// <summary>
/// Optional Elasticsearch configuration that can be provided in request payloads.
/// If not provided, defaults from local.settings.json will be used.
/// </summary>
public sealed record ElasticsearchConfig
{
    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; init; }

    [JsonPropertyName("indexName")]
    public string? IndexName { get; init; }

    /// <summary>
    /// Merges this config with fallback values from configuration.
    /// </summary>
    /// <param name="fallbackUri">Fallback URI from configuration</param>
    /// <param name="fallbackApiKey">Fallback API key from configuration</param>
    /// <param name="fallbackIndexName">Fallback index name from configuration</param>
    /// <returns>Merged configuration with non-null values</returns>
    public ElasticsearchConfig MergeWithFallbacks(string fallbackUri, string? fallbackApiKey, string fallbackIndexName)
    {
        return new ElasticsearchConfig
        {
            Uri = !string.IsNullOrWhiteSpace(Uri) ? Uri : fallbackUri,
            ApiKey = !string.IsNullOrWhiteSpace(ApiKey) ? ApiKey : fallbackApiKey,
            IndexName = !string.IsNullOrWhiteSpace(IndexName) ? IndexName : fallbackIndexName
        };
    }
}