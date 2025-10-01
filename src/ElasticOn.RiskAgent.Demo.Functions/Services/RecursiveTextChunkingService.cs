using RecursiveTextSplitting;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

/// <summary>
/// Text chunking service implementation using the RecursiveTextSplitter library.
/// Provides semantic-aware text splitting that preserves natural boundaries like paragraphs,
/// sentences, and words, falling back to character-level splitting only when necessary.
/// </summary>
public class RecursiveTextChunkingService : ITextChunkingService
{
    /// <summary>
    /// Splits text into chunks using recursive semantic-aware splitting.
    /// The RecursiveTextSplitter preserves natural text boundaries (paragraphs, sentences, words)
    /// and only falls back to character-level splitting as a last resort.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkSize">Maximum size of each chunk in characters.</param>
    /// <param name="overlapSize">Number of characters to overlap between consecutive chunks.</param>
    /// <returns>Array of text chunks.</returns>
    public string[] ChunkText(string text, int chunkSize, int overlapSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        // Use RecursiveTextSplitter extension method for semantic-aware splitting
        var chunks = text.RecursiveSplit(chunkSize: chunkSize, chunkOverlap: overlapSize);
        
        return chunks.ToArray();
    }

    /// <summary>
    /// Processes multiple pages of text, chunking each page and calculating statistics.
    /// Uses semantic-aware chunking that respects natural text boundaries.
    /// </summary>
    /// <param name="pages">Array of page texts to chunk.</param>
    /// <param name="chunkSize">Maximum size of each chunk in characters.</param>
    /// <param name="overlapSize">Number of characters to overlap between consecutive chunks.</param>
    /// <returns>Chunking statistics including page count, chunk counts, and averages.</returns>
    public ChunkingStats ChunkPages(string[] pages, int chunkSize, int overlapSize)
    {
        if (pages == null || pages.Length == 0)
        {
            return new ChunkingStats(0, Array.Empty<int>(), 0, 0, 0);
        }

        var chunksPerPage = new int[pages.Length];
        int totalChunks = 0;

        for (int i = 0; i < pages.Length; i++)
        {
            var chunks = ChunkText(pages[i], chunkSize, overlapSize);
            chunksPerPage[i] = chunks.Length;
            totalChunks += chunks.Length;
        }

        double avgChunksPerPage = pages.Length > 0 ? (double)totalChunks / pages.Length : 0;
        int minChunks = chunksPerPage.Length > 0 ? chunksPerPage.Min() : 0;
        int maxChunks = chunksPerPage.Length > 0 ? chunksPerPage.Max() : 0;

        return new ChunkingStats(
            PageCount: pages.Length,
            ChunksPerPage: chunksPerPage,
            AvgChunksPerPage: avgChunksPerPage,
            MinChunksInPage: minChunks,
            MaxChunksInPage: maxChunks
        );
    }
}
