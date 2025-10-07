using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElasticOn.RiskAgent.Demo.M365.Services;

/// <summary>
/// Service for Azure AI Content Safety operations including jailbreak detection
/// </summary>
public class ContentSafetyService : IContentSafetyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentSafetyService> _logger;
    private readonly string _endpoint;
    private readonly string _subscriptionKey;
    private const int MaxPromptLength = 1000;
    private const string ApiVersion = "2024-09-01";

    /// <summary>
    /// Gets the current jailbreak detection mode
    /// </summary>
    public JailbreakDetectionMode DetectionMode { get; }

    public ContentSafetyService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ContentSafetyService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ContentSafetyClient");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Read detection mode configuration
        var modeString = Environment.GetEnvironmentVariable("AZURE_CONTENT_SAFETY_JAILBREAK_DETECTION_MODE")
            ?? configuration["AIServicesContentSafetyJailbreakDetectionMode"]
            ?? "Enforce"; // Default to Enforce for backward compatibility

        if (!Enum.TryParse<JailbreakDetectionMode>(modeString, true, out var mode))
        {
            _logger.LogWarning("Invalid jailbreak detection mode '{Mode}', defaulting to Enforce", modeString);
            mode = JailbreakDetectionMode.Enforce;
        }

        DetectionMode = mode;

        // Read Content Safety configuration - only required if not disabled
        if (DetectionMode != JailbreakDetectionMode.Disabled)
        {
            _endpoint = Environment.GetEnvironmentVariable("AZURE_CONTENT_SAFETY_ENDPOINT")
                ?? configuration["AIServicesContentSafetyEndpoint"]
                ?? throw new InvalidOperationException("AZURE_CONTENT_SAFETY_ENDPOINT is not set.");

            _subscriptionKey = Environment.GetEnvironmentVariable("AZURE_CONTENT_SAFETY_SUBSCRIPTION_KEY")
                ?? configuration["AIServicesContentSafetySubscriptionKey"]
                ?? throw new InvalidOperationException("AZURE_CONTENT_SAFETY_SUBSCRIPTION_KEY is not set.");

            _logger.LogInformation("ContentSafetyService initialized with endpoint: {Endpoint}, jailbreak detection mode: {Mode}", 
                _endpoint, DetectionMode);
        }
        else
        {
            _endpoint = string.Empty;
            _subscriptionKey = string.Empty;
            _logger.LogInformation("ContentSafetyService initialized with jailbreak detection mode: Disabled");
        }
    }

    /// <summary>
    /// Analyzes text for potential jailbreak attempts using Azure Content Safety Prompt Shield.
    /// Text longer than 1000 characters is automatically split into chunks and analyzed separately.
    /// </summary>
    /// <param name="text">The text to analyze (will be chunked internally if needed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detection result including whether jailbreak was detected and offending text</returns>
    public async Task<JailbreakDetectionResult> DetectJailbreakAsync(
        string text, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty text provided to DetectJailbreakAsync");
            return new JailbreakDetectionResult
            {
                IsJailbreakDetected = false,
                Mode = DetectionMode
            };
        }

        try
        {
            // Split text into chunks if needed
            var textChunks = SplitTextIntoChunks(text, MaxPromptLength);
            _logger.LogDebug("Text split into {ChunkCount} chunk(s) for analysis", textChunks.Count);

            // Process each chunk
            foreach (var chunk in textChunks)
            {
                var detected = await AnalyzeChunkAsync(chunk, [chunk], cancellationToken);
                
                if (detected)
                {
                    _logger.LogWarning("Jailbreak attempt detected in text chunk");
                    return new JailbreakDetectionResult
                    {
                        IsJailbreakDetected = true,
                        OffendingText = chunk,
                        Mode = DetectionMode
                    };
                }
            }

            _logger.LogDebug("No jailbreak detected in any chunks");
            return new JailbreakDetectionResult
            {
                IsJailbreakDetected = false,
                Mode = DetectionMode
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception while calling Content Safety API");
            // Fail open - don't block user on service errors
            return new JailbreakDetectionResult
            {
                IsJailbreakDetected = false,
                Mode = DetectionMode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during jailbreak detection");
            // Fail open - don't block user on unexpected errors
            return new JailbreakDetectionResult
            {
                IsJailbreakDetected = false,
                Mode = DetectionMode
            };
        }
    }

    /// <summary>
    /// Analyzes a single chunk of text using the Content Safety API
    /// </summary>
    private async Task<bool> AnalyzeChunkAsync(
        string userPrompt,
        string[] documents,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build the request
            var request = new ShieldPromptRequest(userPrompt, documents);
            var payload = JsonSerializer.Serialize(request, JsonSerializerOptions.Default);

            var url = $"{_endpoint}/contentsafety/text:shieldPrompt?api-version={ApiVersion}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending jailbreak detection request for chunk (length: {Length})", userPrompt.Length);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ShieldPromptResponse>(resultJson);

                return result?.UserPromptAnalysis?.Detected ?? false;
            }
            else
            {
                _logger.LogError("Content Safety API returned error status: {StatusCode}", response.StatusCode);
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Error details: {ErrorContent}", errorContent);
                
                // In case of API failure, we fail open (return false) to avoid blocking legitimate requests
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing chunk");
            return false;
        }
    }

    /// <summary>
    /// Splits text into chunks of maximum length
    /// </summary>
    private static List<string> SplitTextIntoChunks(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string>();
        }

        if (text.Length <= maxLength)
        {
            return new List<string> { text };
        }

        var chunks = new List<string>();
        var currentPosition = 0;

        while (currentPosition < text.Length)
        {
            var remainingLength = text.Length - currentPosition;
            var chunkLength = Math.Min(maxLength, remainingLength);
            chunks.Add(text.Substring(currentPosition, chunkLength));
            currentPosition += chunkLength;
        }

        return chunks;
    }

    #region Request/Response Models

    /// <summary>
    /// Request model for Shield Prompt API
    /// </summary>
    private class ShieldPromptRequest
    {
        [JsonPropertyName("userPrompt")]
        public string UserPrompt { get; set; }

        [JsonPropertyName("documents")]
        public string[] Documents { get; set; }

        public ShieldPromptRequest(string userPrompt, string[] documents)
        {
            UserPrompt = userPrompt ?? throw new ArgumentNullException(nameof(userPrompt));
            Documents = documents ?? throw new ArgumentNullException(nameof(documents));
            
            if (documents.Length == 0)
            {
                throw new ArgumentException("At least one document must be provided.", nameof(documents));
            }
        }
    }

    /// <summary>
    /// Response model for Shield Prompt API
    /// </summary>
    private class ShieldPromptResponse
    {
        [JsonPropertyName("userPromptAnalysis")]
        public JailbreakAnalysis? UserPromptAnalysis { get; set; }

        [JsonPropertyName("documentsAnalysis")]
        public DocumentAnalysis[]? DocumentsAnalysis { get; set; }
    }

    /// <summary>
    /// Jailbreak analysis result
    /// </summary>
    private class JailbreakAnalysis
    {
        [JsonPropertyName("attackDetected")]
        public bool Detected { get; set; }
    }

    /// <summary>
    /// Document analysis result
    /// </summary>
    private class DocumentAnalysis
    {
        [JsonPropertyName("attackDetected")]
        public bool Detected { get; set; }
    }

    #endregion
}
