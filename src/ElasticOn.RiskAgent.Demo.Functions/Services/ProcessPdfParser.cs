using ElasticOn.RiskAgent.Demo.Functions.Models;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

internal static class ProcessPdfParser
{

    public static ProcessPdfData Parse(string fileContent, DocumentMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            throw new ArgumentException("fileContent is required", nameof(fileContent));
        }

        ArgumentNullException.ThrowIfNull(metadata);

        byte[] pdfBytes = DecodeBase64(fileContent);

        // Extract text from the PDF
        string firstPageText = PdfTextExtractor.ExtractFirstPageText(pdfBytes);
        int pageCount = PdfTextExtractor.GetPageCount(pdfBytes);

        return new ProcessPdfData(pdfBytes, metadata, firstPageText, pageCount);
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
    string FirstPageText, 
    int PageCount);
