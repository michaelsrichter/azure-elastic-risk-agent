using ElasticOn.RiskAgent.Demo.Functions.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace ElasticOn.RiskAgent.Demo.Functions.Services;

public class ProcessPdfParser
{
    private readonly ITextChunkingService _chunkingService;

    public ProcessPdfParser(ITextChunkingService chunkingService)
    {
        _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
    }

    // Overload for backward compatibility with tests
    public ProcessPdfData Parse(string fileContent, DocumentMetadata metadata)
    {
        // Create default configuration for testing
        var configDict = new Dictionary<string, string?>
        {
            ["ChunkSize"] = "500",
            ["ChunkOverlap"] = "50"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        
        return Parse(fileContent, metadata, configuration);
    }

    // Overload with HttpClient for backward compatibility with tests
    public ProcessPdfData Parse(string fileContent, DocumentMetadata metadata, HttpClient? httpClient)
    {
        // Create default configuration for testing
        var configDict = new Dictionary<string, string?>
        {
            ["ChunkSize"] = "500",
            ["ChunkOverlap"] = "50"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
        
        return Parse(fileContent, metadata, configuration, httpClient);
    }

    public ProcessPdfData Parse(string fileContent, DocumentMetadata metadata, IConfiguration configuration)
    {
        return Parse(fileContent, metadata, configuration, httpClientFactory: null);
    }

    public ProcessPdfData Parse(string fileContent, DocumentMetadata metadata, IConfiguration configuration, HttpClient? httpClient, ElasticsearchConfig? elasticsearchConfig = null)
    {
        return Parse(fileContent, metadata, configuration, httpClientFactory: null, elasticsearchConfig, httpClient);
    }

    public ProcessPdfData Parse(string fileContent, DocumentMetadata metadata, IConfiguration configuration, IHttpClientFactory? httpClientFactory, ElasticsearchConfig? elasticsearchConfig = null, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            throw new ArgumentException("fileContent is required", nameof(fileContent));
        }

        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(configuration);

        byte[] pdfBytes = DecodeBase64(fileContent);

        // Extract text from all pages of the PDF
        string[] pageTexts = PdfTextExtractor.ExtractTextFromAllPages(pdfBytes);

        // Get chunking configuration
        int chunkSize = int.Parse(configuration["ChunkSize"] ?? "500");
        int chunkOverlap = int.Parse(configuration["ChunkOverlap"] ?? "50");

        // Perform text chunking and get statistics
        ChunkingStats chunkingStats = _chunkingService.ChunkPages(pageTexts, chunkSize, chunkOverlap);

        // Index chunks if HttpClientFactory or HttpClient is provided
        if (httpClientFactory != null || httpClient != null)
        {
            _ = Task.Run(async () => await IndexChunksAsync(pageTexts, metadata, chunkSize, chunkOverlap, httpClientFactory, httpClient, elasticsearchConfig, configuration));
        }

        return new ProcessPdfData(pdfBytes, metadata, chunkingStats);
    }

    private async Task IndexChunksAsync(string[] pageTexts, DocumentMetadata metadata, int chunkSize, int chunkOverlap, IHttpClientFactory? httpClientFactory, HttpClient? httpClient, ElasticsearchConfig? elasticsearchConfig, IConfiguration configuration)
    {
        try
        {
            HttpClient clientToUse;
            bool shouldDisposeClient = false;
            
            // If an HttpClient was explicitly provided (like in tests), use it
            if (httpClient != null)
            {
                clientToUse = httpClient;
                Console.WriteLine("Using explicitly provided HttpClient");
            }
            else
            {
                // Check environment for appropriate HttpClient creation
                var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
                if (string.IsNullOrEmpty(websiteHostname) || websiteHostname.Contains("localhost"))
                {
                    // Local development - create a simple HttpClient with no SSL complications
                    var handler = new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                    clientToUse = new HttpClient(handler);
                    shouldDisposeClient = true;
                    Console.WriteLine("Created simple HttpClient for local development");
                }
                else
                {
                    // Production - use the factory client
                    var factoryClient = httpClientFactory?.CreateClient("default");
                    clientToUse = factoryClient ?? throw new InvalidOperationException("No HttpClient available for indexing");
                    Console.WriteLine("Using factory HttpClient for production");
                }
            }

            try
            {
                for (int pageIndex = 0; pageIndex < pageTexts.Length; pageIndex++)
                {
                    var pageText = pageTexts[pageIndex];
                    var chunks = _chunkingService.ChunkText(pageText, chunkSize, chunkOverlap);

                    for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                    {
                        var indexRequest = new IndexDocumentRequest
                        {
                            DocumentMetadata = metadata,
                            PageNumber = pageIndex + 1, // 1-based page numbering
                            PageChunkNumber = chunkIndex + 1, // 1-based chunk numbering
                            Chunk = chunks[chunkIndex],
                            ElasticsearchConfig = elasticsearchConfig
                        };

                        await CallIndexDocumentFunction(clientToUse, indexRequest, configuration);
                    }
                }
            }
            finally
            {
                // Only dispose if we created the HttpClient ourselves
                if (shouldDisposeClient)
                {
                    clientToUse?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception but don't throw to avoid breaking the main PDF processing
            // In a real application, you might want to use proper logging here
            Console.WriteLine($"Error indexing chunks: {ex.Message}");
        }
    }

    private async Task CallIndexDocumentFunction(HttpClient httpClient, IndexDocumentRequest request, IConfiguration configuration)
    {
        // Get retry configuration with defaults
        int maxRetries = int.Parse(configuration["IndexDocumentMaxRetries"] ?? "3");
        int initialDelayMs = int.Parse(configuration["IndexDocumentInitialRetryDelayMs"] ?? "1000");
        
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        // Always use HTTP for local development to avoid SSL issues
        string baseUrl;
        var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        var azureFunctionsEnvironment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        var aspnetcoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var internalFunctionKey = Environment.GetEnvironmentVariable("INTERNAL_FUNCTION_KEY");
        
        Console.WriteLine($"Environment variables - WEBSITE_HOSTNAME: {websiteHostname}, AZURE_FUNCTIONS_ENVIRONMENT: {azureFunctionsEnvironment}, ASPNETCORE_ENVIRONMENT: {aspnetcoreEnvironment}");
        
        // Get URL path from configuration with fallback to default
        string urlPath = configuration["IndexDocumentUrlPath"] ?? "/api/index-document";
        
        if (!string.IsNullOrEmpty(websiteHostname) && !websiteHostname.Contains("localhost"))
        {
            // Production Azure Functions environment
            baseUrl = $"https://{websiteHostname}";
            Console.WriteLine($"Using production base address: {baseUrl}");
            
            // Add function key for authentication in production
            if (!string.IsNullOrEmpty(internalFunctionKey) && internalFunctionKey != "placeholder-will-be-updated-after-deployment")
            {
                urlPath += $"?code={internalFunctionKey}";
                Console.WriteLine($"Added function key to URL for authentication");
            }
            else
            {
                Console.WriteLine($"WARNING: INTERNAL_FUNCTION_KEY not configured - authentication may fail");
            }
        }
        else
        {
            // Local development - always use HTTP, never HTTPS
            baseUrl = "http://localhost:7071";
            Console.WriteLine($"Using local development base address: {baseUrl}");
        }

        var fullUrl = $"{baseUrl}{urlPath}";
        
        // Hide key in logs if present
        var logUrl = fullUrl;
        if (!string.IsNullOrEmpty(internalFunctionKey) && internalFunctionKey != "placeholder-will-be-updated-after-deployment")
        {
            logUrl = fullUrl.Replace(internalFunctionKey, "***");
        }
        
        // Set base address if not already set, then use relative path
        if (httpClient.BaseAddress == null)
        {
            httpClient.BaseAddress = new Uri(baseUrl);
        }

        // Retry loop with exponential backoff
        int attempt = 0;
        Exception? lastException = null;
        
        while (attempt <= maxRetries)
        {
            try
            {
                attempt++;
                
                if (attempt > 1)
                {
                    Console.WriteLine($"Retry attempt {attempt} of {maxRetries + 1} for document {request.DocumentMetadata.Id}, page {request.PageNumber}, chunk {request.PageChunkNumber}");
                }
                else
                {
                    Console.WriteLine($"Calling IndexDocumentFunction at: {logUrl}");
                }
                
                // Create fresh content for each attempt
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(urlPath, content);
                
                if (response.IsSuccessStatusCode)
                {
                    if (attempt > 1)
                    {
                        Console.WriteLine($"Successfully indexed chunk after {attempt} attempts for document {request.DocumentMetadata.Id}, page {request.PageNumber}, chunk {request.PageChunkNumber}");
                    }
                    else
                    {
                        Console.WriteLine($"Successfully indexed chunk for document {request.DocumentMetadata.Id}, page {request.PageNumber}, chunk {request.PageChunkNumber}");
                    }
                    return; // Success - exit retry loop
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"Failed to index chunk: {response.StatusCode} - {errorContent}";
                    Console.WriteLine(errorMessage);
                    
                    // Check if this is a retryable error (5xx server errors or 429 rate limit)
                    bool isRetryable = ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600) || 
                                      response.StatusCode == System.Net.HttpStatusCode.TooManyRequests;
                    
                    if (!isRetryable || attempt > maxRetries)
                    {
                        Console.WriteLine($"Non-retryable error or max retries exceeded. Giving up on document {request.DocumentMetadata.Id}, page {request.PageNumber}, chunk {request.PageChunkNumber}");
                        return; // Non-retryable error or out of retries
                    }
                    
                    // Wait before retry with exponential backoff
                    int delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    Console.WriteLine($"Waiting {delayMs}ms before retry...");
                    await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"Error calling IndexDocumentFunction (attempt {attempt}): {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                if (attempt > maxRetries)
                {
                    Console.WriteLine($"Max retries exceeded. Giving up on document {request.DocumentMetadata.Id}, page {request.PageNumber}, chunk {request.PageChunkNumber}");
                    return;
                }
                
                // Wait before retry with exponential backoff
                int delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                Console.WriteLine($"Waiting {delayMs}ms before retry...");
                await Task.Delay(delayMs);
            }
        }
    }

    private byte[] DecodeBase64(string input)
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

public sealed record ProcessPdfData(
    byte[] PdfBytes, 
    DocumentMetadata Metadata, 
    ChunkingStats ChunkingStats);
