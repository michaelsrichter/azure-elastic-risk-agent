using Elastic.Clients.Elasticsearch;
using ElasticOn.RiskAgent.Demo.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

internal interface IElasticsearchService
{
    Task<bool> IndexDocumentAsync(ElasticsearchDocument document, CancellationToken cancellationToken = default);
    Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
}

internal sealed class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        
        var elasticsearchUri = configuration["ElasticsearchUri"] ?? "http://localhost:9200";
        var apiKey = configuration["ElasticsearchApiKey"];
        _indexName = configuration["ElasticsearchIndexName"] ?? "risk-agent-documents";

        var settings = new ElasticsearchClientSettings(new Uri(elasticsearchUri));

        if (!string.IsNullOrEmpty(apiKey))
        {
            settings.Authentication(new Elastic.Transport.ApiKey(apiKey));
        }

        _client = new ElasticsearchClient(settings);
    }

    public async Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking if index '{IndexName}' exists", _indexName);

            var existsResponse = await _client.Indices.ExistsAsync(_indexName, cancellationToken);

            if (existsResponse.Exists)
            {
                _logger.LogInformation("Index '{IndexName}' already exists", _indexName);
                return true;
            }

            _logger.LogInformation("Index '{IndexName}' does not exist, creating it", _indexName);

            var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                ), cancellationToken);

            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("Successfully created index '{IndexName}'", _indexName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to create index '{IndexName}'. Error: {Error}", 
                    _indexName, createResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while ensuring index '{IndexName}' exists", _indexName);
            return false;
        }
    }

    public async Task<bool> IndexDocumentAsync(ElasticsearchDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Indexing document with ID: {DocumentId}", document.Id);

            // Ensure index exists before indexing document
            var indexExists = await EnsureIndexExistsAsync(cancellationToken);
            if (!indexExists)
            {
                _logger.LogError("Failed to ensure index exists before indexing document with ID: {DocumentId}", document.Id);
                return false;
            }

            var response = await _client.IndexAsync(
                document,
                idx => idx
                    .Index(_indexName)
                    .Id(document.Id)
                    .OpType(OpType.Index), // This allows for overwriting existing documents
                cancellationToken);

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully indexed document with ID: {DocumentId}", document.Id);
                return true;
            }
            else
            {
                _logger.LogError("Failed to index document with ID: {DocumentId}. Error: {Error}", 
                    document.Id, response.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while indexing document with ID: {DocumentId}", document.Id);
            return false;
        }
    }
}