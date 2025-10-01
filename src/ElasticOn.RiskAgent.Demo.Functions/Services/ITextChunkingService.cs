namespace ElasticOn.RiskAgent.Demo.Functions.Services;

/// <summary>
/// Service interface for chunking text with configurable size and overlap.
/// </summary>
public interface ITextChunkingService
{
    /// <summary>
    /// Splits text into chunks of approximately the specified size with optional overlap between chunks.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkSize">Maximum size of each chunk in characters.</param>
    /// <param name="overlapSize">Number of characters to overlap between consecutive chunks.</param>
    /// <returns>Array of text chunks.</returns>
    string[] ChunkText(string text, int chunkSize, int overlapSize);

    /// <summary>
    /// Processes multiple pages of text, chunking each page and calculating statistics.
    /// </summary>
    /// <param name="pages">Array of page texts to chunk.</param>
    /// <param name="chunkSize">Maximum size of each chunk in characters.</param>
    /// <param name="overlapSize">Number of characters to overlap between consecutive chunks.</param>
    /// <returns>Chunking statistics including page count, chunk counts, and averages.</returns>
    ChunkingStats ChunkPages(string[] pages, int chunkSize, int overlapSize);
}

/// <summary>
/// Statistics about text chunking across multiple pages.
/// </summary>
/// <param name="PageCount">Total number of pages processed.</param>
/// <param name="ChunksPerPage">Number of chunks created for each page.</param>
/// <param name="AvgChunksPerPage">Average number of chunks per page.</param>
/// <param name="MinChunksInPage">Minimum chunks found in any single page.</param>
/// <param name="MaxChunksInPage">Maximum chunks found in any single page.</param>
public record ChunkingStats(
    int PageCount,
    int[] ChunksPerPage,
    double AvgChunksPerPage,
    int MinChunksInPage,
    int MaxChunksInPage
);
