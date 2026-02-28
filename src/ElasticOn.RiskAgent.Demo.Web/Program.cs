using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ElasticOn.RiskAgent.Demo.Web;
using ElasticOn.RiskAgent.Demo.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API calls
// The BaseAddress will be configured to point to the Azure Function API endpoint
builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress)
});

// Register ChatStateService as a singleton to maintain state across the app
builder.Services.AddSingleton<ChatStateService>();

// Register ClarityService for analytics tracking
builder.Services.AddScoped<ClarityService>();

// Register LocalizationService as scoped (in Blazor WASM, scoped = singleton lifetime)
builder.Services.AddScoped<LocalizationService>();

// Register AgentConfigDialogService for nav-to-page communication
builder.Services.AddScoped<AgentConfigDialogService>();

var host = builder.Build();

// Initialize Microsoft Clarity tracking
var clarityService = host.Services.GetRequiredService<ClarityService>();
await clarityService.InitializeAsync();

// Initialize localization (reads saved locale from localStorage)
var localizationService = host.Services.GetRequiredService<LocalizationService>();
await localizationService.InitializeAsync();

await host.RunAsync();
