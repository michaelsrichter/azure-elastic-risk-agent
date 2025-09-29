using ElasticOn.RiskAgent.Demo.Functions.Models;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElasticOn.RiskAgent.Demo.Functions.Tests;

public class IndexDocumentFunctionTests
{
    private readonly Mock<ILogger<ElasticsearchService>> _mockLogger;
    private readonly Mock<IElasticsearchService> _mockElasticsearchService;

    public IndexDocumentFunctionTests()
    {
        _mockLogger = new Mock<ILogger<ElasticsearchService>>();
        _mockElasticsearchService = new Mock<IElasticsearchService>();
    }

    [Fact]
    public void ElasticsearchDocument_FromRequest_GeneratesCorrectId()
    {
        // Arrange
        var request = new IndexDocumentRequest
        {
            DocumentMetadata = new DocumentMetadata
            {
                Id = "test-doc-id",
                FilenameWithExtension = "test.pdf",
                FullPath = "/documents/test.pdf",
                VersionNumber = "1.0",
                Modified = DateTimeOffset.UtcNow,
                Created = DateTimeOffset.UtcNow,
                Link = "https://example.com/test.pdf"
            },
            PageNumber = 1,
            PageChunkNumber = 1,
            Chunk = "Test chunk content"
        };

        // Act
        var document = ElasticsearchDocument.FromRequest(request);

        // Assert
        Assert.NotNull(document.Id);
        Assert.NotEqual(string.Empty, document.Id);
        Assert.Equal(request.DocumentMetadata.FilenameWithExtension, document.FilenameWithExtension);
        Assert.Equal(request.DocumentMetadata.FullPath, document.FullPath);
        Assert.Equal(request.DocumentMetadata.VersionNumber, document.VersionNumber);
        Assert.Equal(request.PageNumber, document.PageNumber);
        Assert.Equal(request.PageChunkNumber, document.PageChunkNumber);
        Assert.Equal(request.Chunk, document.Chunk);
    }

    [Fact]
    public void ElasticsearchDocument_GenerateId_SameInputProducesSameId()
    {
        // Arrange
        var filename = "test.pdf";
        var pageNumber = 1;
        var pageChunkNumber = 1;

        // Act
        var id1 = ElasticsearchDocument.GenerateId(filename, pageNumber, pageChunkNumber);
        var id2 = ElasticsearchDocument.GenerateId(filename, pageNumber, pageChunkNumber);

        // Assert
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ElasticsearchDocument_GenerateId_DifferentInputsProduceDifferentIds()
    {
        // Arrange & Act
        var id1 = ElasticsearchDocument.GenerateId("test.pdf", 1, 1);
        var id2 = ElasticsearchDocument.GenerateId("test.pdf", 1, 2);
        var id3 = ElasticsearchDocument.GenerateId("test.pdf", 2, 1);
        var id4 = ElasticsearchDocument.GenerateId("other.pdf", 1, 1);

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id1, id3);
        Assert.NotEqual(id1, id4);
        Assert.NotEqual(id2, id3);
        Assert.NotEqual(id2, id4);
        Assert.NotEqual(id3, id4);
    }

    [Fact]
    public void ElasticsearchDocument_GenerateId_ThrowsExceptionForEmptyFilename()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ElasticsearchDocument.GenerateId("", 1, 1));
        Assert.Throws<ArgumentException>(() => ElasticsearchDocument.GenerateId(null, 1, 1));
    }

    [Fact]
    public async Task ElasticsearchService_IndexDocumentAsync_CallsExpectedMethods()
    {
        // Arrange
        var document = new ElasticsearchDocument
        {
            Id = "test-id",
            FilenameWithExtension = "test.pdf",
            PageNumber = 1,
            PageChunkNumber = 1,
            Chunk = "Test chunk"
        };

        _mockElasticsearchService
            .Setup(x => x.IndexDocumentAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockElasticsearchService.Object.IndexDocumentAsync(document, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockElasticsearchService.Verify(
            x => x.IndexDocumentAsync(document, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ElasticsearchService_EnsureIndexExistsAsync_CallsExpectedMethods()
    {
        // Arrange
        _mockElasticsearchService
            .Setup(x => x.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockElasticsearchService.Object.EnsureIndexExistsAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockElasticsearchService.Verify(
            x => x.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}