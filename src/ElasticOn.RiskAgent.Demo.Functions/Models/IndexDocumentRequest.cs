using System.Text.Json.Serialization;

namespace ElasticOn.RiskAgent.Demo.Functions.Models;

internal sealed record IndexDocumentRequest
{
    [JsonPropertyName("documentMetadata")]
    public DocumentMetadata DocumentMetadata { get; init; } = default!;

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("pageChunkNumber")]
    public int PageChunkNumber { get; init; }

    [JsonPropertyName("chunk")]
    public string Chunk { get; init; } = string.Empty;

    [JsonPropertyName("elasticsearchConfig")]
    public ElasticsearchConfig? ElasticsearchConfig { get; init; }

    /// <summary>
    /// Optional: Override the Elasticsearch index name. If provided, takes precedence over
    /// ElasticsearchConfig.IndexName and the configured default.
    /// </summary>
    [JsonPropertyName("elasticsearchIndexName")]
    public string? ElasticsearchIndexName { get; init; }
}