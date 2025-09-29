using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace ElasticOn.RiskAgent.Demo.Functions.Models;

internal sealed record ElasticsearchDocument
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

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; }

    [JsonPropertyName("pageChunkNumber")]
    public int PageChunkNumber { get; init; }

    [JsonPropertyName("chunk")]
    public string Chunk { get; init; } = string.Empty;

    public static string GenerateId(string? filename, int pageNumber, int pageChunkNumber)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
        }

        var input = $"{filename}_{pageNumber}_{pageChunkNumber}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static ElasticsearchDocument FromRequest(IndexDocumentRequest request)
    {
        var id = GenerateId(
            request.DocumentMetadata.FilenameWithExtension,
            request.PageNumber,
            request.PageChunkNumber);

        return new ElasticsearchDocument
        {
            Id = id,
            FilenameWithExtension = request.DocumentMetadata.FilenameWithExtension,
            FullPath = request.DocumentMetadata.FullPath,
            VersionNumber = request.DocumentMetadata.VersionNumber,
            Modified = request.DocumentMetadata.Modified,
            Created = request.DocumentMetadata.Created,
            Link = request.DocumentMetadata.Link,
            PageNumber = request.PageNumber,
            PageChunkNumber = request.PageChunkNumber,
            Chunk = request.Chunk
        };
    }
}