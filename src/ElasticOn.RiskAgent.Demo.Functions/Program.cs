using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ElasticOn.RiskAgent.Demo.Functions.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure JSON serialization to handle camelCase from JavaScript/Blazor
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Configure logging - critical for Azure Functions isolated worker model
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add Application Insights with proper configuration
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure logging filters to ensure Information level logs are captured
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    // Remove default rule that filters out Information logs for Application Insights
    var toRemove = options.Rules.FirstOrDefault(rule => 
        rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (toRemove is not null)
    {
        options.Rules.Remove(toRemove);
    }
    
    // Add explicit rules for our namespaces
    options.Rules.Add(new LoggerFilterRule(
        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
        "ElasticOn.RiskAgent.Demo.Functions",
        LogLevel.Information,
        null));
    
    options.Rules.Add(new LoggerFilterRule(
        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
        "Function",
        LogLevel.Information,
        null));
});

builder.Services
    .AddScoped<IElasticsearchService, ElasticsearchService>();

// Register text chunking service
builder.Services
    .AddSingleton<ITextChunkingService, RecursiveTextChunkingService>();

// Register PDF parser service
builder.Services
    .AddScoped<ProcessPdfParser>();

// Register Azure AI Agent services for Chat function
builder.Services
    .AddSingleton<IAzureAIAgentService, AzureAIAgentService>();

builder.Services
    .AddSingleton<IContentSafetyService, ContentSafetyService>();

// Register conversation state service (in-memory for now)
builder.Services
    .AddSingleton<IChatStateService, ChatStateService>();

// Configure HttpClient with proper SSL handling for development
builder.Services.AddHttpClient("default")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        
        // For development environments, allow untrusted certificates
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        
        return handler;
    });

// Configure HttpClient for Content Safety Service
builder.Services.AddHttpClient("ContentSafetyClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        
        // For development environments, allow untrusted certificates
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        
        return handler;
    });

builder.Build().Run();
