using System.Text.Json.Serialization;

namespace ElasticOn.RiskAgent.Demo.Functions.Models;

internal sealed record DocumentMetadata
{
    [JsonPropertyName("@odata.etag")]
    public string? ETag { get; init; }

    [JsonPropertyName("ItemInternalId")]
    public string? ItemInternalId { get; init; }

    [JsonPropertyName("ID")]
    public int? Id { get; init; }

    [JsonPropertyName("{Name}")]
    public string? Name { get; init; }

    [JsonPropertyName("{FilenameWithExtension}")]
    public string? FilenameWithExtension { get; init; }

    [JsonPropertyName("{FullPath}")]
    public string? FullPath { get; init; }

    [JsonPropertyName("{VersionNumber}")]
    public string? VersionNumber { get; init; }

    [JsonPropertyName("Author")]
    public SharePointUser? Author { get; init; }

    [JsonPropertyName("Editor")]
    public SharePointUser? Editor { get; init; }

    [JsonPropertyName("Modified")]
    public DateTimeOffset? Modified { get; init; }

    [JsonPropertyName("Created")]
    public DateTimeOffset? Created { get; init; }

    [JsonPropertyName("{Link}")]
    public string? Link { get; init; }

    [JsonPropertyName("{DriveId}")]
    public string? DriveId { get; init; }

    [JsonPropertyName("{DriveItemId}")]
    public string? DriveItemId { get; init; }

    [JsonPropertyName("{ContentType}")]
    public SharePointContentType? ContentType { get; init; }
}

internal sealed record SharePointUser
{
    [JsonPropertyName("@odata.type")]
    public string? ODataType { get; init; }

    [JsonPropertyName("Claims")]
    public string? Claims { get; init; }

    [JsonPropertyName("DisplayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("Email")]
    public string? Email { get; init; }

    [JsonPropertyName("Picture")]
    public string? Picture { get; init; }
}

internal sealed record SharePointContentType
{
    [JsonPropertyName("@odata.type")]
    public string? ODataType { get; init; }

    [JsonPropertyName("Id")]
    public string? Id { get; init; }

    [JsonPropertyName("Name")]
    public string? Name { get; init; }
}
