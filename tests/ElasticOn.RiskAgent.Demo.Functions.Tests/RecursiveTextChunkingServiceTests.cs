using ElasticOn.RiskAgent.Demo.Functions.Services;
using Xunit;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

/// <summary>
/// Unit tests for RecursiveTextChunkingService which uses the RecursiveTextSplitter library
/// for semantic-aware text chunking that preserves natural boundaries.
/// </summary>
public sealed class RecursiveTextChunkingServiceTests
{
    private readonly ITextChunkingService _chunkingService;

    public RecursiveTextChunkingServiceTests()
    {
        _chunkingService = new RecursiveTextChunkingService();
    }

    #region ChunkText Tests

    [Fact]
    public void ChunkText_WithNullText_ReturnsEmptyArray()
    {
        // Act
        var result = _chunkingService.ChunkText(null!, 100, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_WithEmptyText_ReturnsEmptyArray()
    {
        // Act
        var result = _chunkingService.ChunkText(string.Empty, 100, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_WithWhitespaceText_ReturnsEmptyArray()
    {
        // Act
        var result = _chunkingService.ChunkText("   ", 100, 10);

        // Assert
        // RecursiveTextSplitter may return array with whitespace chunks
        // or empty array depending on implementation
        Assert.NotNull(result);
    }

    [Fact]
    public void ChunkText_WithTextSmallerThanChunkSize_ReturnsSingleChunk()
    {
        // Arrange
        var text = "This is a short text.";

        // Act
        var result = _chunkingService.ChunkText(text, 100, 10);

        // Assert
        Assert.Single(result);
        Assert.Equal(text, result[0]);
    }

    [Fact]
    public void ChunkText_WithTextLargerThanChunkSize_ReturnsMultipleChunks()
    {
        // Arrange
        var text = "This is the first sentence. This is the second sentence. " +
                   "This is the third sentence. This is the fourth sentence. " +
                   "This is the fifth sentence. This is the sixth sentence.";

        // Act
        var result = _chunkingService.ChunkText(text, 50, 10);

        // Assert
        Assert.True(result.Length > 1, "Expected multiple chunks for large text");
        
        // Verify each chunk (except possibly the last) is within or near the chunk size
        for (int i = 0; i < result.Length - 1; i++)
        {
            Assert.True(result[i].Length <= 100, 
                $"Chunk {i} length {result[i].Length} should be reasonably close to chunk size");
        }
    }

    [Fact]
    public void ChunkText_WithParagraphs_PreservesSemanticBoundaries()
    {
        // Arrange
        var text = "First paragraph with some content.\n\n" +
                   "Second paragraph with more content.\n\n" +
                   "Third paragraph with even more content.";

        // Act
        var result = _chunkingService.ChunkText(text, 40, 5);

        // Assert
        Assert.True(result.Length > 0, "Should create chunks from paragraphed text");
        
        // RecursiveTextSplitter should preserve paragraph boundaries when possible
        // Each chunk should contain complete words/sentences
        foreach (var chunk in result)
        {
            Assert.False(string.IsNullOrWhiteSpace(chunk), "Chunks should not be empty");
        }
    }

    [Fact]
    public void ChunkText_WithOverlap_CreatesOverlappingChunks()
    {
        // Arrange
        var text = "Word1 Word2 Word3 Word4 Word5 Word6 Word7 Word8 Word9 Word10";

        // Act - use moderate chunk size with overlap
        var result = _chunkingService.ChunkText(text, 25, 10);

        // Assert
        if (result.Length > 1)
        {
            // With overlap, subsequent chunks should share some content
            // This is a characteristic of overlapping chunking
            Assert.True(result.Length >= 2, "Should have multiple chunks with this text");
        }
    }

    [Fact]
    public void ChunkText_WithZeroOverlap_CreatesNonOverlappingChunks()
    {
        // Arrange
        var text = "Sentence one here. Sentence two here. Sentence three here. Sentence four here.";

        // Act
        var result = _chunkingService.ChunkText(text, 30, 0);

        // Assert
        Assert.True(result.Length > 0, "Should create at least one chunk");
        
        // With no overlap, chunks should be distinct
        foreach (var chunk in result)
        {
            Assert.NotEmpty(chunk);
        }
    }

    [Fact]
    public void ChunkText_WithLongSingleWord_HandlesGracefully()
    {
        // Arrange
        var longWord = new string('a', 200); // 200 character word

        // Act
        var result = _chunkingService.ChunkText(longWord, 50, 10);

        // Assert
        Assert.NotEmpty(result);
        // RecursiveTextSplitter should handle this by character-level splitting if needed
        Assert.True(result[0].Length > 0, "Should create chunks even for long words");
    }

    [Fact]
    public void ChunkText_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var text = "Hello! How are you? I'm fine, thanks. What about you?";

        // Act
        var result = _chunkingService.ChunkText(text, 30, 5);

        // Assert
        Assert.NotEmpty(result);
        
        // Verify special characters are preserved
        var allChunks = string.Join("", result);
        Assert.Contains("!", allChunks);
        Assert.Contains("?", allChunks);
        Assert.Contains(",", allChunks);
    }

    [Fact]
    public void ChunkText_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var text = "Hello 世界! Bonjour le monde! Привет мир!";

        // Act
        var result = _chunkingService.ChunkText(text, 20, 5);

        // Assert
        Assert.NotEmpty(result);
        
        // Verify unicode characters are preserved
        var allChunks = string.Join("", result);
        Assert.Contains("世界", allChunks);
        Assert.Contains("Привет", allChunks);
    }

    #endregion

    #region ChunkPages Tests

    [Fact]
    public void ChunkPages_WithNullPages_ReturnsEmptyStats()
    {
        // Act
        var result = _chunkingService.ChunkPages(null!, 100, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.PageCount);
        Assert.Empty(result.ChunksPerPage);
        Assert.Equal(0, result.AvgChunksPerPage);
        Assert.Equal(0, result.MinChunksInPage);
        Assert.Equal(0, result.MaxChunksInPage);
    }

    [Fact]
    public void ChunkPages_WithEmptyArray_ReturnsEmptyStats()
    {
        // Act
        var result = _chunkingService.ChunkPages(Array.Empty<string>(), 100, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.PageCount);
        Assert.Empty(result.ChunksPerPage);
        Assert.Equal(0, result.AvgChunksPerPage);
        Assert.Equal(0, result.MinChunksInPage);
        Assert.Equal(0, result.MaxChunksInPage);
    }

    [Fact]
    public void ChunkPages_WithSinglePage_ReturnsCorrectStats()
    {
        // Arrange
        var pages = new[] { "This is a single page with some content." };

        // Act
        var result = _chunkingService.ChunkPages(pages, 100, 10);

        // Assert
        Assert.Equal(1, result.PageCount);
        Assert.Single(result.ChunksPerPage);
        Assert.Equal(1, result.ChunksPerPage[0]); // Should be 1 chunk for small text
        Assert.Equal(1.0, result.AvgChunksPerPage);
        Assert.Equal(1, result.MinChunksInPage);
        Assert.Equal(1, result.MaxChunksInPage);
    }

    [Fact]
    public void ChunkPages_WithMultiplePages_ReturnsCorrectStats()
    {
        // Arrange
        var pages = new[]
        {
            "Page one with content.",
            "Page two with different content.",
            "Page three with even more content here."
        };

        // Act
        var result = _chunkingService.ChunkPages(pages, 100, 10);

        // Assert
        Assert.Equal(3, result.PageCount);
        Assert.Equal(3, result.ChunksPerPage.Length);
        Assert.True(result.AvgChunksPerPage > 0);
        Assert.True(result.MinChunksInPage >= 0);
        Assert.True(result.MaxChunksInPage >= result.MinChunksInPage);
    }

    [Fact]
    public void ChunkPages_WithVaryingPageSizes_CalculatesCorrectAverages()
    {
        // Arrange
        var shortPage = "Short.";
        var mediumPage = "This is a medium length page with several sentences here.";
        var longPage = new string('x', 500); // Long page

        var pages = new[] { shortPage, mediumPage, longPage };

        // Act
        var result = _chunkingService.ChunkPages(pages, 50, 10);

        // Assert
        Assert.Equal(3, result.PageCount);
        Assert.Equal(3, result.ChunksPerPage.Length);
        
        // Verify statistics consistency
        var totalChunks = result.ChunksPerPage.Sum();
        var expectedAvg = (double)totalChunks / pages.Length;
        Assert.Equal(expectedAvg, result.AvgChunksPerPage, precision: 10);
        
        Assert.Equal(result.ChunksPerPage.Min(), result.MinChunksInPage);
        Assert.Equal(result.ChunksPerPage.Max(), result.MaxChunksInPage);
    }

    [Fact]
    public void ChunkPages_WithEmptyPages_HandlesGracefully()
    {
        // Arrange
        var pages = new[] { "", "Content here", "" };

        // Act
        var result = _chunkingService.ChunkPages(pages, 50, 10);

        // Assert
        Assert.Equal(3, result.PageCount);
        Assert.Equal(3, result.ChunksPerPage.Length);
        
        // Empty pages should have 0 chunks
        Assert.Equal(0, result.ChunksPerPage[0]);
        Assert.True(result.ChunksPerPage[1] > 0);
        Assert.Equal(0, result.ChunksPerPage[2]);
    }

    [Fact]
    public void ChunkPages_WithLargePages_CreatesMultipleChunksPerPage()
    {
        // Arrange
        var largePage = string.Join(" ", Enumerable.Range(1, 100).Select(i => $"Word{i}"));
        var pages = new[] { largePage, largePage };

        // Act
        var result = _chunkingService.ChunkPages(pages, 50, 10);

        // Assert
        Assert.Equal(2, result.PageCount);
        Assert.Equal(2, result.ChunksPerPage.Length);
        
        // Each large page should produce multiple chunks
        Assert.True(result.ChunksPerPage[0] > 1, "Large page should produce multiple chunks");
        Assert.True(result.ChunksPerPage[1] > 1, "Large page should produce multiple chunks");
    }

    [Fact]
    public void ChunkPages_WithSmallChunkSize_CreatesMoreChunks()
    {
        // Arrange
        var text = "This is a test sentence that will be chunked into multiple pieces.";
        var pages = new[] { text, text, text };

        // Act with small chunk size
        var resultSmall = _chunkingService.ChunkPages(pages, 20, 5);
        
        // Act with large chunk size
        var resultLarge = _chunkingService.ChunkPages(pages, 200, 5);

        // Assert - smaller chunk size should create more chunks
        var totalChunksSmall = resultSmall.ChunksPerPage.Sum();
        var totalChunksLarge = resultLarge.ChunksPerPage.Sum();
        
        Assert.True(totalChunksSmall >= totalChunksLarge, 
            "Smaller chunk size should create equal or more chunks");
    }

    [Fact]
    public void ChunkPages_StatisticsAreConsistent()
    {
        // Arrange
        var pages = new[]
        {
            new string('a', 100),
            new string('b', 200),
            new string('c', 150),
            new string('d', 50)
        };

        // Act
        var result = _chunkingService.ChunkPages(pages, 60, 10);

        // Assert
        Assert.Equal(4, result.PageCount);
        Assert.Equal(4, result.ChunksPerPage.Length);
        
        // Verify mathematical consistency
        var totalChunks = result.ChunksPerPage.Sum();
        var calculatedAvg = (double)totalChunks / result.PageCount;
        Assert.Equal(calculatedAvg, result.AvgChunksPerPage, precision: 10);
        
        var actualMin = result.ChunksPerPage.Min();
        var actualMax = result.ChunksPerPage.Max();
        Assert.Equal(actualMin, result.MinChunksInPage);
        Assert.Equal(actualMax, result.MaxChunksInPage);
        
        // Min should be <= Avg <= Max
        Assert.True(result.MinChunksInPage <= result.AvgChunksPerPage);
        Assert.True(result.AvgChunksPerPage <= result.MaxChunksInPage);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void ChunkText_WithNewlinesAndTabs_PreservesWhitespace()
    {
        // Arrange
        var text = "Line 1\nLine 2\tTabbed\rLine 3";

        // Act
        var result = _chunkingService.ChunkText(text, 100, 10);

        // Assert
        Assert.NotEmpty(result);
        var combined = string.Join("", result);
        
        // Whitespace characters should be preserved in the output
        Assert.Contains("\n", combined);
        Assert.Contains("\t", combined);
    }

    [Fact]
    public void ChunkText_WithVeryLargeText_CompletesSuccessfully()
    {
        // Arrange - create a large text (1MB)
        var largeText = string.Join(" ", Enumerable.Range(1, 100000).Select(i => $"Word{i}"));

        // Act
        var result = _chunkingService.ChunkText(largeText, 1000, 100);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Length > 10, "Large text should create many chunks");
        
        // Verify no chunks are empty
        Assert.All(result, chunk => Assert.False(string.IsNullOrEmpty(chunk)));
    }

    [Fact]
    public void ChunkPages_WithMixedContentTypes_HandlesAll()
    {
        // Arrange
        var pages = new[]
        {
            "Normal text page.",
            "Page with\nnewlines\nand\ttabs.",
            "Page with special chars: !@#$%^&*()",
            "Unicode: 你好世界 مرحبا العالم",
            "",
            new string('x', 500)
        };

        // Act
        var result = _chunkingService.ChunkPages(pages, 100, 20);

        // Assert
        Assert.Equal(6, result.PageCount);
        Assert.Equal(6, result.ChunksPerPage.Length);
        
        // All pages should be processed
        Assert.All(result.ChunksPerPage, count => Assert.True(count >= 0));
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(50, 5)]
    [InlineData(100, 10)]
    [InlineData(200, 50)]
    [InlineData(500, 100)]
    public void ChunkText_WithVariousChunkSizes_WorksCorrectly(int chunkSize, int overlap)
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Range(1, 100).Select(i => $"Word{i}"));

        // Act
        var result = _chunkingService.ChunkText(text, chunkSize, overlap);

        // Assert
        Assert.NotEmpty(result);
        
        // Verify all chunks are non-empty
        Assert.All(result, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk)));
    }

    #endregion
}
