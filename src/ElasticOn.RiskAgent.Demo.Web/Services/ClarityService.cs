using Microsoft.JSInterop;

namespace ElasticOn.RiskAgent.Demo.Web.Services;

/// <summary>
/// Service to manage Microsoft Clarity tracking.
/// </summary>
public class ClarityService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClarityService> _logger;

    public ClarityService(
        IJSRuntime jsRuntime,
        IConfiguration configuration,
        ILogger<ClarityService> logger)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initializes Microsoft Clarity tracking if enabled in configuration.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var enabled = _configuration.GetValue<bool>("Clarity:Enabled");
            var projectId = _configuration.GetValue<string>("Clarity:ProjectId");

            if (!enabled)
            {
                _logger.LogInformation("Microsoft Clarity tracking is disabled");
                return;
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                _logger.LogWarning("Microsoft Clarity is enabled but ProjectId is not configured");
                return;
            }

            _logger.LogInformation("Initializing Microsoft Clarity tracking with project ID: {ProjectId}", projectId);

            // Inject Clarity script
            await _jsRuntime.InvokeVoidAsync("eval", $@"
                (function(c,l,a,r,i,t,y){{
                    c[a]=c[a]||function(){{(c[a].q=c[a].q||[]).push(arguments)}};
                    t=l.createElement(r);t.async=1;t.src='https://www.clarity.ms/tag/'+i;
                    y=l.getElementsByTagName(r)[0];y.parentNode.insertBefore(t,y);
                }})(window, document, 'clarity', 'script', '{projectId}');
            ");

            _logger.LogInformation("Microsoft Clarity tracking initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Microsoft Clarity tracking");
        }
    }
}
