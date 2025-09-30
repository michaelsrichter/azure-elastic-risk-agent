using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ElasticOn.RiskAgent.Demo.Functions.Functions;

public class HealthCheckFunction
{
    private readonly ILogger<HealthCheckFunction> _logger;

    public HealthCheckFunction(ILogger<HealthCheckFunction> logger)
    {
        _logger = logger;
    }

    [Function("HealthCheck")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        // Log at multiple levels to verify logging is working
        _logger.LogTrace("HealthCheck - TRACE level log");
        _logger.LogDebug("HealthCheck - DEBUG level log");
        _logger.LogInformation("HealthCheck - INFORMATION level log - Request received at {Timestamp}", DateTime.UtcNow);
        _logger.LogWarning("HealthCheck - WARNING level log - This is intentional for testing");
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = "Function App is running. Check Application Insights for logs.",
            logLevelsEmitted = new[] { "Trace", "Debug", "Information", "Warning" }
        });

        _logger.LogInformation("HealthCheck - Response sent successfully");
        
        return response;
    }
}
