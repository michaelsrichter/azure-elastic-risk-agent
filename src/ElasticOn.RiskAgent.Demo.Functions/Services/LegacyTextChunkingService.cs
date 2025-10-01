namespace ElasticOn.RiskAgent.Demo.Functions.Services;

/// <summary>
/// Legacy service for chunking text with configurable chunk size and overlap.
/// This is preserved for reference. Use ITextChunkingService/RecursiveTextChunkingService instead.
/// </summary>
[Obsolete("Use ITextChunkingService with RecursiveTextChunkingService instead")]
internal static class LegacyTextChunkingService
{
    /// <summary>
    /// Chunks text into overlapping segments using an overlapping window approach
    /// </summary>
    /// <param name="text">The text to chunk</param>
    /// <param name="chunkSize">Size of each chunk in characters</param>
    /// <param name="overlapSize">Number of characters to overlap between chunks</param>
    /// <returns>Array of text chunks</returns>
    public static string[] ChunkText(string text, int chunkSize, int overlapSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than 0", nameof(chunkSize));
        }

        if (overlapSize < 0)
        {
            throw new ArgumentException("Overlap size cannot be negative", nameof(overlapSize));
        }

        if (overlapSize >= chunkSize)
        {
            throw new ArgumentException("Overlap size must be less than chunk size", nameof(overlapSize));
        }

        // Implement overlapping window chunking
        var chunks = new List<string>();
        int stepSize = chunkSize - overlapSize;
        
        for (int i = 0; i < text.Length; i += stepSize)
        {
            int actualChunkSize = Math.Min(chunkSize, text.Length - i);
            string chunk = text.Substring(i, actualChunkSize);
            chunks.Add(chunk);
            
            // If this chunk is smaller than the desired chunk size, we've reached the end
            if (actualChunkSize < chunkSize)
            {
                break;
            }
        }
        
        return chunks.ToArray();
    }

    /// <summary>
    /// Chunks multiple pages of text and returns chunking statistics
    /// </summary>
    /// <param name="pages">Array of page texts</param>
    /// <param name="chunkSize">Size of each chunk in characters</param>
    /// <param name="overlapSize">Number of characters to overlap between chunks</param>
    /// <returns>Chunking statistics including chunk counts per page</returns>
    public static ChunkingStats ChunkPages(string[] pages, int chunkSize, int overlapSize)
    {
        if (pages == null || pages.Length == 0)
        {
            return new ChunkingStats(0, Array.Empty<int>(), 0.0, 0, 0);
        }

        var chunksPerPage = new int[pages.Length];
        
        for (int i = 0; i < pages.Length; i++)
        {
            var chunks = ChunkText(pages[i], chunkSize, overlapSize);
            chunksPerPage[i] = chunks.Length;
        }

        var totalChunks = chunksPerPage.Sum();
        var averageChunksPerPage = pages.Length > 0 ? (double)totalChunks / pages.Length : 0.0;
        var maxChunksPerPage = chunksPerPage.Length > 0 ? chunksPerPage.Max() : 0;
        var minChunksPerPage = chunksPerPage.Length > 0 ? chunksPerPage.Min() : 0;

        return new ChunkingStats(pages.Length, chunksPerPage, averageChunksPerPage, maxChunksPerPage, minChunksPerPage);
    }
}