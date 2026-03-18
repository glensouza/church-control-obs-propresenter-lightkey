using System.Text.Json;
using WorshipConsole.Models;

namespace WorshipConsole.Services;

public class ProPresenterService
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly ILogger<ProPresenterService> logger;
    private readonly ProPresenterConfiguration config;

    public ProPresenterService(HttpClient httpClient, IConfiguration configuration, ILogger<ProPresenterService> logger)
    {
        this.httpClient = httpClient;
        this.configuration = configuration;
        this.logger = logger;

        this.config = new ProPresenterConfiguration
        {
            Host = this.configuration["ProPresenter:Host"] ?? "127.0.0.1",
            Port = int.TryParse(this.configuration["ProPresenter:Port"], out int port) ? port : 20000,
            Password = this.configuration["ProPresenter:Password"]
        };

        this.httpClient.BaseAddress = new Uri($"http://{this.config.Host}:{this.config.Port}/");
        this.httpClient.Timeout = TimeSpan.FromSeconds(2);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(this.config.Host);

    public async Task<(bool Success, string Message)> GetStatusAsync()
    {
        try
        {
            string url = this.AppendPassword("version");
            HttpResponseMessage response = await this.httpClient.GetAsync(url);
            return response.IsSuccessStatusCode ? (true, "Connected") : (false, $"Status code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public async Task<(string? Uuid, string? Name, int? Index)> GetActivePresentationDetailsAsync()
    {
        string query = this.GetQueryString();
        string? uuid = null;
        string? name = null;
        int? index = null;

        try
        {
            // 1. Get active presentation structure
            HttpResponseMessage response = await this.httpClient.GetAsync($"v1/presentation/active{query}");
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("presentation", out JsonElement pres))
                {
                    if (pres.TryGetProperty("id", out JsonElement id))
                    {
                        if (id.TryGetProperty("uuid", out JsonElement u)) uuid = u.GetString();
                        if (id.TryGetProperty("name", out JsonElement n)) name = n.GetString();
                        if (id.TryGetProperty("index", out JsonElement i)) index = i.GetInt32();
                    }
                    
                    // Fallback for index
                    if (!index.HasValue && pres.TryGetProperty("slide_index", out JsonElement si)) index = si.GetInt32();
                }
            }

            // 2. If index is still 0 (common bug in some Pro7 versions) or we want to double check,
            // we can try fetching transport state, but since we saw transport was empty in logs, 
            // we'll stick to the active presentation object for now.
        }
        catch { /* Ignore and return what we have */ }

        return (uuid, name, index);
    }

    public async Task<string?> GetThumbnailAsync(string path)
    {
        try
        {
            // Use current ticks as a cache-buster
            string separator = path.Contains('?') ? "&" : "?";
            string url = this.AppendPassword($"{path}{separator}t={DateTime.UtcNow.Ticks}");
            
            HttpResponseMessage response = await this.httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            return data.Length == 0 ? null : $"data:image/jpeg;base64,{Convert.ToBase64String(data)}";
        }
        catch { return null; }
    }

    public async Task<bool> NextSlideAsync() => await this.SendTriggerAsync("v1/presentation/active/next/trigger");
    public async Task<bool> PreviousSlideAsync() => await this.SendTriggerAsync("v1/presentation/active/previous/trigger");

    public async Task<bool> ClearAllAsync()
    {
        // Multi-layer clear for thoroughness
        await this.SendTriggerAsync("v1/clear/layer/media");
        await this.SendTriggerAsync("v1/clear/layer/video_input");
        return await this.SendTriggerAsync("v1/clear/layer/slide");
    }

    public async Task<bool> ClearSlideAsync() => await this.SendTriggerAsync("v1/clear/layer/slide");

    private async Task<bool> SendTriggerAsync(string url)
    {
        try
        {
            HttpResponseMessage response = await this.httpClient.GetAsync(this.AppendPassword(url));
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private string GetQueryString() => string.IsNullOrEmpty(this.config.Password) ? "" : $"?password={Uri.EscapeDataString(this.config.Password)}";

    private string AppendPassword(string path)
    {
        if (string.IsNullOrEmpty(this.config.Password)) return path;
        string separator = path.Contains('?') ? "&" : "?";
        return $"{path}{separator}password={Uri.EscapeDataString(this.config.Password)}";
    }
}
