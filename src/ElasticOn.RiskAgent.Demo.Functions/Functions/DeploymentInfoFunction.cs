using System.Net;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ElasticOn.RiskAgent.Demo.Functions.Functions;

public sealed class DeploymentInfoFunction
{
    private readonly ILogger<DeploymentInfoFunction> _logger;
    
    // This constant will be updated at build time to show when the code was compiled
    private static readonly string BuildTimestamp = "BUILD_TIMESTAMP_PLACEHOLDER";
    private static readonly string BuildVersion = "1.0.1";

    public DeploymentInfoFunction(ILogger<DeploymentInfoFunction> logger)
    {
        _logger = logger;
    }

    [Function("DeploymentInfo")]
    [OpenApiOperation(operationId: "DeploymentInfo", tags: new[] { "System" }, Summary = "Get deployment information", Description = "Returns information about the current deployment including build timestamp and version")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "Deployment information retrieved successfully")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "deployment-info")] HttpRequestData request)
    {
        _logger.LogInformation("DeploymentInfo endpoint called at {Timestamp}", DateTime.UtcNow);

        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();
        var assemblyBuildDate = GetAssemblyBuildDate(assembly);
        
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = "deployed",
            buildTimestamp = BuildTimestamp,
            buildVersion = BuildVersion,
            assemblyVersion = assemblyName.Version?.ToString() ?? "unknown",
            assemblyBuildDate = assemblyBuildDate?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "unknown",
            currentTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            environment = new
            {
                websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "localhost",
                azureFunctionsEnvironment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development",
                runFromPackage = Environment.GetEnvironmentVariable("WEBSITE_RUN_FROM_PACKAGE") ?? "not set"
            }
        });

        return response;
    }

    private static DateTime? GetAssemblyBuildDate(Assembly assembly)
    {
        try
        {
            var buildDateAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
            if (buildDateAttribute?.Key == "BuildDate" && !string.IsNullOrEmpty(buildDateAttribute.Value))
            {
                return DateTime.Parse(buildDateAttribute.Value);
            }

            // Fallback: use the last write time of the assembly file
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                return File.GetLastWriteTimeUtc(location);
            }
        }
        catch
        {
            // Ignore errors and return null
        }

        return null;
    }
}
