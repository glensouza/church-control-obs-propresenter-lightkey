using System.Net.Http.Json;
using System.Text.Json;
using WorshipConsole.Models;

namespace WorshipConsole.Services;

public class ProPresenterService
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly ILogger<ProPresenterService> logger;
    private readonly ProPresenterConfiguration config;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        string? requestUrl = null;
        try
        {
            // Try standard version endpoint
            string url = "version";
            if (!string.IsNullOrEmpty(this.config.Password))
            {
                url += $"?password={Uri.EscapeDataString(this.config.Password)}";
            }
            
            requestUrl = $"{this.httpClient.BaseAddress}{url}";
            HttpResponseMessage response = await this.httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "Connected");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Try a fallback to see if the API is even there
                HttpResponseMessage docResponse = await this.httpClient.GetAsync("openapi.json");
                if (docResponse.IsSuccessStatusCode)
                {
                    return (false, "API found but /version 404ed. Try updating ProPresenter.");
                }
                return (false, "404 Not Found. You might be hitting the 'Remote' port instead of the 'Network API' port. Check ProPresenter settings.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (false, "Unauthorized (401). Check your API password in appsettings.json.");
            }

            string error = $"ProPresenter returned {response.StatusCode} ({(int)response.StatusCode})";
            this.logger.LogWarning("{Error} for {Url}", error, requestUrl);
            return (false, error);
        }
        catch (HttpRequestException ex)
        {
            string error;
            string message = ex.Message;
            string? innerMessage = ex.InnerException?.Message;

            bool looksNonHttp = message.Contains("invalid status line", StringComparison.OrdinalIgnoreCase)
                                || (innerMessage != null && innerMessage.Contains("invalid status line", StringComparison.OrdinalIgnoreCase));

            if (looksNonHttp)
            {
                error = "Received a non-HTTP response. Use the ProPresenter Network API port (default 20000) with API enabled; avoid the TCP/IP, Remote, or Stage App ports.";
            }
            else
            {
                error = $"Network error: {message}";
                if (innerMessage != null) error += $" ({innerMessage})";
            }

            this.logger.LogWarning("HTTP error connecting to ProPresenter at {Url}: {Message}", requestUrl ?? "unknown", error);
            return (false, error);
        }
        catch (TaskCanceledException)
        {
            string error = "Connection timed out (2s). Is the IP/Port correct?";
            return (false, error);
        }
        catch (Exception ex)
        {
            string error = $"Unexpected error: {ex.Message}";
            this.logger.LogWarning(ex, "Unexpected error connecting to ProPresenter at {Url}", requestUrl ?? "unknown");
            return (false, error);
        }
    }

    public async Task<bool> NextSlideAsync()
    {
        return await this.SendTriggerAsync("v1/presentation/active/next/trigger");
    }

    public async Task<bool> PreviousSlideAsync()
    {
        return await this.SendTriggerAsync("v1/presentation/active/previous/trigger");
    }

    public async Task<bool> TriggerSlideAsync(int index)
    {
        return await this.SendTriggerAsync($"v1/presentation/active/{index}/trigger");
    }

    public async Task<int?> GetActiveSlideIndexAsync()
    {
        try
        {
            string query = string.IsNullOrEmpty(this.config.Password) ? "" : $"?password={Uri.EscapeDataString(this.config.Password)}";
            HttpResponseMessage response = await this.httpClient.GetAsync($"v1/presentation/active/slide_index{query}");
            if (!response.IsSuccessStatusCode) return null;

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("index", out JsonElement indexProp))
            {
                return indexProp.GetInt32();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetThumbnailAsync(string path)
    {
        try
        {
            string query = string.IsNullOrEmpty(this.config.Password) ? "" : $"?password={Uri.EscapeDataString(this.config.Password)}";
            HttpResponseMessage response = await this.httpClient.GetAsync($"{path}{query}");
            if (!response.IsSuccessStatusCode) return null;

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            return $"data:image/jpeg;base64,{Convert.ToBase64String(data)}";
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ClearAllAsync()
    {
        return await this.SendTriggerAsync("v1/clear/all");
    }

    public async Task<bool> ClearSlideAsync()
    {
        return await this.SendTriggerAsync("v1/clear/layer/slide");
    }

    private async Task<bool> SendTriggerAsync(string url)
    {
        try
        {
            if (!string.IsNullOrEmpty(this.config.Password))
            {
                url += $"?password={Uri.EscapeDataString(this.config.Password)}";
            }
            HttpResponseMessage response = await this.httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error triggering ProPresenter endpoint {Url}", url);
            return false;
        }
    }
}
