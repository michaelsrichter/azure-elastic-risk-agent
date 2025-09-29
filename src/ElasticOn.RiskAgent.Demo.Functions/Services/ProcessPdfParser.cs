using System.Text.Json;
using System.Text.Json.Nodes;
using ElasticOn.RiskAgent.Demo.Functions.Models;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

internal static class ProcessPdfParser
{
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ProcessPdfData Parse(string fileContent, string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            throw new ArgumentException("fileContent is required", nameof(fileContent));
        }

        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            throw new ArgumentException("metadata is required", nameof(metadataJson));
        }

    byte[] pdfBytes = DecodeBase64(fileContent);

        JsonNode? node = JsonNode.Parse(metadataJson);
        if (node is not JsonObject metadataObject)
        {
            throw new JsonException("Metadata must deserialize to a JSON object.");
        }

        DocumentMetadata? metadata = JsonSerializer.Deserialize<DocumentMetadata>(metadataJson, MetadataSerializerOptions);
        if (metadata is null)
        {
            throw new JsonException("Unable to deserialize metadata into DocumentMetadata.");
        }

        // Extract text from the PDF
        string firstPageText = PdfTextExtractor.ExtractFirstPageText(pdfBytes);
        int pageCount = PdfTextExtractor.GetPageCount(pdfBytes);

        return new ProcessPdfData(pdfBytes, metadataObject, metadata, firstPageText, pageCount);
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
    JsonObject MetadataObject, 
    DocumentMetadata Metadata, 
    string FirstPageText, 
    int PageCount);
