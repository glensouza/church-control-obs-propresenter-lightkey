using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WorshipConsole.Models;

namespace WorshipConsole.Services;

public class UniFiService
{
    private readonly ILogger<UniFiService> logger;
    private readonly UniFiConfiguration config;
    private readonly HttpClient httpClient;
    private string? authCookie;
    private string? csrfToken;
    private string? deviceId;
    private string apiPrefix = string.Empty;
    private readonly SemaphoreSlim authLock = new(1, 1);

    public UniFiService(ILogger<UniFiService> logger, IConfiguration configuration)
    {
        this.logger = logger;
        this.config = configuration.GetSection("UniFi").Get<UniFiConfiguration>() ?? new UniFiConfiguration();

        HttpClientHandler handler = new();
        if (this.config.IgnoreSslErrors)
        {
            this.logger.LogWarning("UniFi TLS certificate validation is disabled (IgnoreSslErrors=true). Do not use this setting in production.");
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        this.httpClient = new HttpClient(handler);
        if (!string.IsNullOrWhiteSpace(this.config.Host))
        {
            this.httpClient.BaseAddress = new Uri(this.config.Host);
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(this.config.Host)
        && !string.IsNullOrWhiteSpace(this.config.Username)
        && !string.IsNullOrWhiteSpace(this.config.Password)
        && !string.IsNullOrWhiteSpace(this.config.SwitchMac);

    private async Task<bool> AuthenticateAsync()
    {
        await this.authLock.WaitAsync();
        try
        {
            if (this.authCookie != null)
            {
                return true;
            }

            string loginPayload = JsonSerializer.Serialize(new { username = this.config.Username, password = this.config.Password });
            StringContent content = new(loginPayload, Encoding.UTF8, "application/json");

            // Try UniFi OS endpoint first
            HttpResponseMessage response = await this.httpClient.PostAsync("/api/auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                this.apiPrefix = "/proxy/network";
                this.ExtractAuthFromResponse(response);
                if (this.authCookie != null)
                {
                    this.logger.LogInformation("Authenticated to UniFi OS");
                    return true;
                }
                this.logger.LogWarning("UniFi OS login succeeded but no auth cookie was returned");
            }

            // Fallback to classic controller
            content = new StringContent(loginPayload, Encoding.UTF8, "application/json");
            response = await this.httpClient.PostAsync("/api/login", content);
            if (response.IsSuccessStatusCode)
            {
                this.apiPrefix = string.Empty;
                this.ExtractAuthFromResponse(response);
                if (this.authCookie != null)
                {
                    this.logger.LogInformation("Authenticated to UniFi classic controller");
                    return true;
                }
                this.logger.LogWarning("UniFi classic login succeeded but no auth cookie was returned");
            }

            this.logger.LogWarning("UniFi authentication failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error authenticating to UniFi");
            return false;
        }
        finally
        {
            this.authLock.Release();
        }
    }

    private void ExtractAuthFromResponse(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies))
        {
            foreach (string cookie in cookies)
            {
                if (cookie.StartsWith("unifises=") || cookie.StartsWith("TOKEN="))
                {
                    this.authCookie = cookie.Split(';')[0];
                    break;
                }
            }
        }

        if (response.Headers.TryGetValues("X-CSRF-Token", out IEnumerable<string>? csrfValues))
        {
            this.csrfToken = csrfValues.FirstOrDefault();
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        HttpRequestMessage request = new(method, $"{this.apiPrefix}{path}");
        if (this.authCookie != null)
        {
            request.Headers.Add("Cookie", this.authCookie);
        }
        if (this.csrfToken != null)
        {
            request.Headers.Add("X-CSRF-Token", this.csrfToken);
        }
        return request;
    }

    private async Task<HttpResponseMessage?> SendWithAuthAsync(Func<HttpRequestMessage> requestFactory)
    {
        if (this.authCookie == null && !await this.AuthenticateAsync())
        {
            return null;
        }

        HttpResponseMessage response = await this.httpClient.SendAsync(requestFactory());
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            this.authCookie = null;
            this.csrfToken = null;
            if (!await this.AuthenticateAsync())
            {
                return null;
            }
            response = await this.httpClient.SendAsync(requestFactory());
        }

        return response;
    }

    public async Task<List<PortStatus>> GetAllPortsStatusAsync()
    {
        List<PortStatus> ports = [];
        if (!this.IsConfigured)
        {
            return ports;
        }

        try
        {
            HttpResponseMessage? response = await this.SendWithAuthAsync(() => this.CreateRequest(HttpMethod.Get, $"/api/s/{this.config.SiteName}/stat/device"));
            if (response is not { IsSuccessStatusCode: true })
            {
                this.logger.LogWarning("Failed to get device stats: {Status}", response?.StatusCode);
                return ports;
            }

            string json = await response.Content.ReadAsStringAsync();
            JsonNode? root = JsonNode.Parse(json);
            JsonArray? data = root?["data"]?.AsArray();
            if (data == null)
            {
                return ports;
            }

            string switchMacNormalized = this.config.SwitchMac.ToLowerInvariant().Replace("-", ":");
            foreach (JsonNode? device in data)
            {
                string? mac = device?["mac"]?.GetValue<string>().ToLowerInvariant();
                if (mac != switchMacNormalized)
                {
                    continue;
                }

                this.deviceId = device?["_id"]?.GetValue<string>();
                JsonArray? portTable = device?["port_table"]?.AsArray();
                if (portTable == null)
                {
                    continue;
                }

                foreach (JsonNode? port in portTable)
                {
                    string? poeMode = port?["poe_mode"]?.GetValue<string>();
                    if (poeMode == null)
                    {
                        continue;
                    }

                    ports.Add(new PortStatus
                    {
                        PortNumber = port?["port_idx"]?.GetValue<int>() ?? 0,
                        PortName = port?["name"]?.GetValue<string>() ?? string.Empty,
                        IsEnabled = port?["up"]?.GetValue<bool>() ?? false,
                        PoeEnabled = poeMode != "off"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting port statuses from UniFi");
        }

        return ports;
    }

    public async Task<bool> SetPortStateAsync(int portNumber, bool enabled)
    {
        if (!this.IsConfigured)
        {
            return false;
        }

        try
        {
            if (this.deviceId == null)
            {
                await this.GetAllPortsStatusAsync();
                if (this.deviceId == null)
                {
                    this.logger.LogWarning("Device ID not found; cannot set port state");
                    return false;
                }
            }

            var payload = new
            {
                port_overrides = new[]
                {
                    new { port_idx = portNumber, poe_mode = enabled ? "auto" : "off" }
                }
            };
            string json = JsonSerializer.Serialize(payload);

            HttpResponseMessage? response = await this.SendWithAuthAsync(() =>
            {
                HttpRequestMessage req = this.CreateRequest(HttpMethod.Put, $"/api/s/{this.config.SiteName}/rest/device/{this.deviceId}");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            });

            if (response is not { IsSuccessStatusCode: true })
            {
                this.logger.LogWarning("Failed to set port {Port} state: {Status}", portNumber, response?.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error setting port {Port} state", portNumber);
            return false;
        }
    }
}
