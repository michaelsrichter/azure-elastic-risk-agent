using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace ElasticOn.RiskAgent.Demo.Web.Services;

/// <summary>
/// JSON-based localization service for Blazor WebAssembly.
/// Loads locale JSON files from wwwroot/locales/ and stores the user's preference in localStorage.
/// Uses a dedicated HttpClient with the app's base URI (not the API base URL).
/// </summary>
public class LocalizationService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    private Dictionary<string, string> _strings = new();
    private string _currentLocale = "en-US";

    public string CurrentLocale => _currentLocale;

    public event Action? OnLanguageChanged;

    public static readonly (string Code, string Label)[] SupportedLocales = new[]
    {
        ("en-US", "English"),
        ("ja-JP", "日本語")
    };

    public LocalizationService(NavigationManager nav, IJSRuntime js)
    {
        // Create a dedicated HttpClient that points to the app's own origin,
        // not the API base URL configured for the main HttpClient.
        _http = new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
        _js = js;
    }

    /// <summary>
    /// Initialize the service: read saved preference from localStorage, then load the locale file.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var saved = await _js.InvokeAsync<string?>("localStorage.getItem", "locale");
            if (!string.IsNullOrEmpty(saved) && SupportedLocales.Any(l => l.Code == saved))
            {
                _currentLocale = saved;
            }
        }
        catch
        {
            // localStorage may not be available during prerendering
        }

        await LoadStringsAsync();
    }

    /// <summary>
    /// Change the active locale, persist to localStorage, and notify listeners.
    /// </summary>
    public async Task SetLocaleAsync(string locale)
    {
        if (_currentLocale == locale) return;
        _currentLocale = locale;

        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "locale", locale);
        }
        catch { /* ignore */ }

        await LoadStringsAsync();
        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// Get a localized string by key. Returns the key itself if not found.
    /// </summary>
    public string this[string key] =>
        _strings.TryGetValue(key, out var value) ? value : key;

    private async Task LoadStringsAsync()
    {
        try
        {
            var dict = await _http.GetFromJsonAsync<Dictionary<string, string>>($"locales/{_currentLocale}.json");
            _strings = dict ?? new Dictionary<string, string>();
        }
        catch
        {
            _strings = new Dictionary<string, string>();
        }
    }
}
