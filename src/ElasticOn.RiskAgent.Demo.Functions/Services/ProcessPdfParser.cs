using ElasticOn.RiskAgent.Demo.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

internal static class ProcessPdfParser
{
    // Overload for backward compatibility with tests
    public static ProcessPdfData Parse(string fileContent, DocumentMetadata metadata)
    {
        // Create default configuration for testing
        var configDict = new Dictionary<string, string?>
        {
            ["ChunkSize"] = "500",
            ["ChunkOverlap"] = "50"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        
        return Parse(fileContent, metadata, configuration);
    }

    public static ProcessPdfData Parse(string fileContent, DocumentMetadata metadata, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            throw new ArgumentException("fileContent is required", nameof(fileContent));
        }

        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(configuration);

        byte[] pdfBytes = DecodeBase64(fileContent);

        // Extract text from all pages of the PDF
        string[] pageTexts = PdfTextExtractor.ExtractTextFromAllPages(pdfBytes);

        // Get chunking configuration
        int chunkSize = int.Parse(configuration["ChunkSize"] ?? "500");
        int chunkOverlap = int.Parse(configuration["ChunkOverlap"] ?? "50");

        // Perform text chunking and get statistics
        ChunkingStats chunkingStats = TextChunkingService.ChunkPages(pageTexts, chunkSize, chunkOverlap);

        return new ProcessPdfData(pdfBytes, metadata, chunkingStats);
    }

    private static byte[] DecodeBase64(string input)
    {
        string normalized = input.Trim();

        if (normalized.Length == 0)
        {
            throw new FormatException("Base64 content cannot be empty.");
        }

        normalized = normalized.Replace('-', '+').Replace('_', '/');
        normalized = normalized.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty);

        int paddingNeeded = normalized.Length % 4;
        if (paddingNeeded != 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - paddingNeeded), '=');
        }

        return Convert.FromBase64String(normalized);
    }
}

internal sealed record ProcessPdfData(
    byte[] PdfBytes, 
    DocumentMetadata Metadata, 
    ChunkingStats ChunkingStats);
