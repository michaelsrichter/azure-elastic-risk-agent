using System.Text.Json.Serialization;

namespace ElasticOn.RiskAgent.Demo.Functions.Models;

internal sealed record DocumentMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("filenameWithExtension")]
    public string? FilenameWithExtension { get; init; }

    [JsonPropertyName("fullPath")]
    public string? FullPath { get; init; }

    [JsonPropertyName("versionNumber")]
    public string? VersionNumber { get; init; }

    [JsonPropertyName("modified")]
    public DateTimeOffset? Modified { get; init; }

    [JsonPropertyName("created")]
    public DateTimeOffset? Created { get; init; }

    [JsonPropertyName("link")]
    public string? Link { get; init; }
}
