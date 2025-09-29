using Elastic.Clients.Elasticsearch;
using ElasticOn.RiskAgent.Demo.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

internal interface IElasticsearchService
{
    Task<bool> IndexDocumentAsync(ElasticsearchDocument document, CancellationToken cancellationToken = default);
    Task<bool> IndexDocumentAsync(ElasticsearchDocument document, ElasticsearchConfig? customConfig, CancellationToken cancellationToken = default);
    Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> EnsureIndexExistsAsync(ElasticsearchConfig? customConfig, CancellationToken cancellationToken = default);
}

internal sealed class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;
    private readonly string _elasticsearchUri;
    private readonly string? _apiKey;
    private readonly string? _azureOpenAiInferenceId;

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        
        _elasticsearchUri = configuration["ElasticsearchUri"] ?? "http://localhost:9200";
        _apiKey = configuration["ElasticsearchApiKey"];
        _indexName = configuration["ElasticsearchIndexName"] ?? "risk-agent-documents";
        _azureOpenAiInferenceId = configuration["AzureOpenAiInferenceId"];

        _logger.LogInformation("Elasticsearch configuration - Uri: {Uri}, IndexName: {IndexName}, HasApiKey: {HasApiKey}", 
            _elasticsearchUri, _indexName, !string.IsNullOrEmpty(_apiKey));

        var settings = new ElasticsearchClientSettings(new Uri(_elasticsearchUri));

        if (!string.IsNullOrEmpty(_apiKey))
        {
            settings.Authentication(new Elastic.Transport.ApiKey(_apiKey));
            _logger.LogInformation("API Key authentication configured");
        }
        else
        {
            _logger.LogWarning("No API Key provided - using anonymous access");
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
            _logger.LogInformation("Using Azure OpenAI inference endpoint: {InferenceId}", _azureOpenAiInferenceId ?? "azure-openai-inference");

            // Create index with semantic text mapping for Azure OpenAI
            // Note: Removed shard/replica settings for serverless compatibility
            var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
                .Mappings(m => m
                    .Properties(ps => ps
                        .Text("chunk", t => t // For the source "chunk" field
                            .CopyTo("semantic_chunk") // Copy content to semantic_chunk field
                        )
                        .SemanticText("semantic_chunk", st => st // For the semantic_chunk field
                            .InferenceId(_azureOpenAiInferenceId ?? "azure-openai-inference") // reference Azure OpenAI endpoint id
                        )
                    )
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

    public async Task<bool> IndexDocumentAsync(ElasticsearchDocument document, ElasticsearchConfig? customConfig, CancellationToken cancellationToken = default)
    {
        if (customConfig == null)
        {
            return await IndexDocumentAsync(document, cancellationToken);
        }

        var mergedConfig = customConfig.MergeWithFallbacks(_elasticsearchUri, _apiKey, _indexName);
        var client = CreateClientFromConfig(customConfig);
        var indexName = mergedConfig.IndexName!;

        try
        {
            _logger.LogInformation("Indexing document with ID: {DocumentId} using custom config", document.Id);

            // Ensure index exists before indexing document
            var indexExists = await EnsureIndexExistsAsync(customConfig, cancellationToken);
            if (!indexExists)
            {
                _logger.LogError("Failed to ensure index exists before indexing document with ID: {DocumentId}", document.Id);
                return false;
            }

            var response = await client.IndexAsync(
                document,
                idx => idx
                    .Index(indexName)
                    .Id(document.Id)
                    .OpType(OpType.Index),
                cancellationToken);

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully indexed document with ID: {DocumentId} using custom config", document.Id);
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
            _logger.LogError(ex, "Exception occurred while indexing document with ID: {DocumentId} using custom config", document.Id);
            return false;
        }
    }

    public async Task<bool> EnsureIndexExistsAsync(ElasticsearchConfig? customConfig, CancellationToken cancellationToken = default)
    {
        if (customConfig == null)
        {
            return await EnsureIndexExistsAsync(cancellationToken);
        }

        var mergedConfig = customConfig.MergeWithFallbacks(_elasticsearchUri, _apiKey, _indexName);
        var client = CreateClientFromConfig(customConfig);
        var indexName = mergedConfig.IndexName!;

        try
        {
            _logger.LogInformation("Checking if index '{IndexName}' exists using custom config", indexName);

            var existsResponse = await client.Indices.ExistsAsync(indexName, cancellationToken);

            if (existsResponse.Exists)
            {
                _logger.LogInformation("Index '{IndexName}' already exists", indexName);
                return true;
            }

            _logger.LogInformation("Index '{IndexName}' does not exist, creating it", indexName);

            // Create index with semantic text mapping for Azure OpenAI
            // Note: Removed shard/replica settings for serverless compatibility
            var createResponse = await client.Indices.CreateAsync(indexName, c => c
                .Mappings(m => m
                    .Properties(ps => ps
                        .Text("chunk", t => t // For the source "chunk" field
                            .CopyTo("semantic_chunk") // Copy content to semantic_chunk field
                        )
                        .SemanticText("semantic_chunk", st => st // For the semantic_chunk field
                            .InferenceId(_azureOpenAiInferenceId ?? "azure-openai-inference") // reference Azure OpenAI endpoint id
                        )
                    )
                ), cancellationToken);

            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("Successfully created index '{IndexName}' using custom config", indexName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to create index '{IndexName}'. Error: {Error}", 
                    indexName, createResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while ensuring index '{IndexName}' exists using custom config", indexName);
            return false;
        }
    }

    private ElasticsearchClient CreateClientFromConfig(ElasticsearchConfig config)
    {
        // Merge custom config with fallback values from configuration
        var mergedConfig = config.MergeWithFallbacks(_elasticsearchUri, _apiKey, _indexName);
        
        var settings = new ElasticsearchClientSettings(new Uri(mergedConfig.Uri!));

        if (!string.IsNullOrEmpty(mergedConfig.ApiKey))
        {
            settings.Authentication(new Elastic.Transport.ApiKey(mergedConfig.ApiKey));
        }

        return new ElasticsearchClient(settings);
    }
}