using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

internal static class PdfTextExtractor
{
    /// <summary>
    /// Extracts text from all pages of a PDF document
    /// </summary>
    /// <param name="pdfBytes">The PDF document as byte array</param>
    /// <returns>Array of strings, one for each page</returns>
    public static string[] ExtractTextFromAllPages(byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new ArgumentException("PDF bytes cannot be null or empty", nameof(pdfBytes));
        }

        try
        {
            // Try with parsing options to handle PDFs with font issues more gracefully
            var options = new ParsingOptions 
            { 
                UseLenientParsing = true,
                SkipMissingFonts = true 
            };
            using var document = PdfDocument.Open(pdfBytes, options);
            var pageTexts = new string[document.NumberOfPages];

            for (int i = 0; i < document.NumberOfPages; i++)
            {
                try
                {
                    var page = document.GetPage(i + 1); // PdfPig uses 1-based page numbering
                    var text = ExtractTextFromPage(page);
                    pageTexts[i] = text;
                }
                catch (Exception pageEx)
                {
                    // If we can't extract text from a specific page, add error message but continue
                    pageTexts[i] = $"[Error extracting page {i + 1}: {pageEx.Message}]";
                }
            }

            return pageTexts;
        }
        catch (Exception ex)
        {
            // If we can't open the PDF at all, return a fallback result
            return new[] { $"[PDF text extraction failed: {ex.Message}]" };
        }
    }

    /// <summary>
    /// Helper method to extract text from a single page using multiple methods
    /// </summary>
    /// <param name="page">The PDF page to extract text from</param>
    /// <returns>Extracted text or appropriate fallback message</returns>
    private static string ExtractTextFromPage(Page page)
    {
        // Method 1: Use the built-in Text property
        var text = page.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Method 2: Extract text from individual words
        try
        {
            var words = page.GetWords();
            if (words.Any())
            {
                return string.Join(" ", words.Select(w => w.Text));
            }
        }
        catch (Exception wordEx)
        {
            System.Diagnostics.Debug.WriteLine($"Word extraction failed: {wordEx.Message}");
        }

        // Method 3: Extract text from letters (more granular)
        try
        {
            var letters = page.Letters;
            if (letters.Any())
            {
                // Group letters into words based on position
                var extractedText = string.Join("", letters.OrderBy(l => l.GlyphRectangle.Bottom)
                                                        .ThenBy(l => l.GlyphRectangle.Left)
                                                        .Select(l => l.Value));
                
                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    return extractedText;
                }
            }
        }
        catch (Exception letterEx)
        {
            System.Diagnostics.Debug.WriteLine($"Letter extraction failed: {letterEx.Message}");
        }

        return "[No text content found in page]";
    }

    /// <summary>
    /// Extracts text from the first page of a PDF document
    /// </summary>
    /// <param name="pdfBytes">The PDF document as byte array</param>
    /// <returns>Text content of the first page, or empty string if no text found</returns>
    public static string ExtractFirstPageText(byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new ArgumentException("PDF bytes cannot be null or empty", nameof(pdfBytes));
        }

        try
        {
            // Try with parsing options to handle PDFs with font issues more gracefully
            var options = new ParsingOptions 
            { 
                UseLenientParsing = true,
                SkipMissingFonts = true 
            };
            using var document = PdfDocument.Open(pdfBytes, options);
            
            if (document.NumberOfPages == 0)
            {
                return string.Empty;
            }

            try
            {
                var firstPage = document.GetPage(1); // PdfPig uses 1-based page numbering
                
                // Try multiple extraction methods
                // Method 1: Use the built-in Text property
                var text = firstPage.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }

                // Method 2: Extract text from individual words
                try
                {
                    var words = firstPage.GetWords();
                    if (words.Any())
                    {
                        return string.Join(" ", words.Select(w => w.Text));
                    }
                }
                catch (Exception wordEx)
                {
                    // Log word extraction failure but continue to method 3
                    System.Diagnostics.Debug.WriteLine($"Word extraction failed: {wordEx.Message}");
                }

                // Method 3: Extract text from letters (more granular)
                try
                {
                    var letters = firstPage.Letters;
                    if (letters.Any())
                    {
                        // Group letters into words based on position
                        var extractedText = string.Join("", letters.OrderBy(l => l.GlyphRectangle.Bottom)
                                                                .ThenBy(l => l.GlyphRectangle.Left)
                                                                .Select(l => l.Value));
                        
                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            return extractedText;
                        }
                    }
                }
                catch (Exception letterEx)
                {
                    // Log letter extraction failure but continue
                    System.Diagnostics.Debug.WriteLine($"Letter extraction failed: {letterEx.Message}");
                }

                // Method 4: Raw text extraction from PDF content streams (fallback for font issues)
                try
                {
                    var rawText = ExtractRawTextFromPdf(pdfBytes);
                    if (!string.IsNullOrWhiteSpace(rawText))
                    {
                        return rawText;
                    }
                }
                catch (Exception rawEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Raw text extraction failed: {rawEx.Message}");
                }

                return "[No text content found in PDF]";
            }
            catch (Exception pageEx)
            {
                // If we can't extract text from the first page, return a fallback
                return $"[Error extracting first page text: {pageEx.Message}]";
            }
        }
        catch (Exception ex)
        {
            // If we can't open the PDF at all, return a fallback result
            return $"[PDF text extraction failed: {ex.Message}]";
        }
    }

    /// <summary>
    /// Gets the total number of pages in a PDF document
    /// </summary>
    /// <param name="pdfBytes">The PDF document as byte array</param>
    /// <returns>Number of pages in the document</returns>
    public static int GetPageCount(byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new ArgumentException("PDF bytes cannot be null or empty", nameof(pdfBytes));
        }

        try
        {
            // Try with parsing options to handle PDFs with font issues more gracefully
            var options = new ParsingOptions 
            { 
                UseLenientParsing = true,
                SkipMissingFonts = true 
            };
            using var document = PdfDocument.Open(pdfBytes, options);
            return document.NumberOfPages;
        }
        catch (Exception)
        {
            // If we can't open the PDF, return 0 as a fallback
            return 0;
        }
    }

    /// <summary>
    /// Raw text extraction from PDF content streams - fallback for PDFs with font loading issues
    /// </summary>
    /// <param name="pdfBytes">The PDF document as byte array</param>
    /// <returns>Raw text extracted from PDF content streams</returns>
    private static string ExtractRawTextFromPdf(byte[] pdfBytes)
    {
        try
        {
            var pdfContent = Encoding.UTF8.GetString(pdfBytes);
            
            // Look for text commands in PDF content streams
            // PDF text is typically shown with commands like:
            // (Text Content) Tj  - Show text
            // [(Text)] TJ        - Show text array
            
            var extractedTexts = new List<string>();
            
            // Pattern to match text between parentheses followed by Tj command
            var tjPattern = @"\(([^)]+)\)\s*Tj";
            var tjMatches = Regex.Matches(pdfContent, tjPattern, RegexOptions.IgnoreCase);
            foreach (Match match in tjMatches)
            {
                extractedTexts.Add(match.Groups[1].Value);
            }
            
            // Pattern to match text arrays with TJ command
            var tjArrayPattern = @"\[\s*\(([^)]+)\)\s*\]\s*TJ";
            var tjArrayMatches = Regex.Matches(pdfContent, tjArrayPattern, RegexOptions.IgnoreCase);
            foreach (Match match in tjArrayMatches)
            {
                extractedTexts.Add(match.Groups[1].Value);
            }
            
            // Also look for simpler patterns where text might be directly readable
            // This handles cases like our test PDF which has readable text in the stream
            var readableTextPattern = @"\([A-Za-z0-9\s]+\)";
            var readableMatches = Regex.Matches(pdfContent, readableTextPattern);
            foreach (Match match in readableMatches)
            {
                var text = match.Value.Trim('(', ')');
                if (text.Length > 2 && !extractedTexts.Contains(text))  // Avoid single chars and duplicates
                {
                    extractedTexts.Add(text);
                }
            }
            
            if (extractedTexts.Any())
            {
                return string.Join(" ", extractedTexts.Distinct());
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Raw PDF text extraction failed: {ex.Message}");
            return string.Empty;
        }
    }
}