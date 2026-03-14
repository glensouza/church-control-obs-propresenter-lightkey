using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WorshipConsole.Models;

namespace WorshipConsole.Services;

public class UniFiService
{
    private readonly ILogger<UniFiService> _logger;
    private readonly UniFiConfiguration _config;
    private readonly HttpClient _httpClient;
    private string? _authCookie;
    private string? _csrfToken;
    private string? _deviceId;
    private string _apiPrefix = string.Empty;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    public UniFiService(ILogger<UniFiService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration.GetSection("UniFi").Get<UniFiConfiguration>() ?? new UniFiConfiguration();

        var handler = new HttpClientHandler();
        if (_config.IgnoreSslErrors)
        {
            _logger.LogWarning("UniFi TLS certificate validation is disabled (IgnoreSslErrors=true). Do not use this setting in production.");
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        _httpClient = new HttpClient(handler);
        if (!string.IsNullOrWhiteSpace(_config.Host))
        {
            _httpClient.BaseAddress = new Uri(_config.Host);
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.Host)
        && !string.IsNullOrWhiteSpace(_config.Username)
        && !string.IsNullOrWhiteSpace(_config.Password)
        && !string.IsNullOrWhiteSpace(_config.SwitchMac);

    private async Task<bool> AuthenticateAsync()
    {
        await _authLock.WaitAsync();
        try
        {
            if (_authCookie != null)
            {
                return true;
            }

            var loginPayload = JsonSerializer.Serialize(new { username = _config.Username, password = _config.Password });
            var content = new StringContent(loginPayload, Encoding.UTF8, "application/json");

            // Try UniFi OS endpoint first
            var response = await _httpClient.PostAsync("/api/auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                _apiPrefix = "/proxy/network";
                ExtractAuthFromResponse(response);
                if (_authCookie != null)
                {
                    _logger.LogInformation("Authenticated to UniFi OS");
                    return true;
                }
                _logger.LogWarning("UniFi OS login succeeded but no auth cookie was returned");
            }

            // Fallback to classic controller
            content = new StringContent(loginPayload, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync("/api/login", content);
            if (response.IsSuccessStatusCode)
            {
                _apiPrefix = string.Empty;
                ExtractAuthFromResponse(response);
                if (_authCookie != null)
                {
                    _logger.LogInformation("Authenticated to UniFi classic controller");
                    return true;
                }
                _logger.LogWarning("UniFi classic login succeeded but no auth cookie was returned");
            }

            _logger.LogWarning("UniFi authentication failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating to UniFi");
            return false;
        }
        finally
        {
            _authLock.Release();
        }
    }

    private void ExtractAuthFromResponse(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.StartsWith("unifises=") || cookie.StartsWith("TOKEN="))
                {
                    _authCookie = cookie.Split(';')[0];
                    break;
                }
            }
        }

        if (response.Headers.TryGetValues("X-CSRF-Token", out var csrfValues))
        {
            _csrfToken = csrfValues.FirstOrDefault();
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{_apiPrefix}{path}");
        if (_authCookie != null)
        {
            request.Headers.Add("Cookie", _authCookie);
        }
        if (_csrfToken != null)
        {
            request.Headers.Add("X-CSRF-Token", _csrfToken);
        }
        return request;
    }

    private async Task<HttpResponseMessage?> SendWithAuthAsync(Func<HttpRequestMessage> requestFactory)
    {
        if (_authCookie == null && !await AuthenticateAsync())
        {
            return null;
        }

        var response = await _httpClient.SendAsync(requestFactory());
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            _authCookie = null;
            _csrfToken = null;
            if (!await AuthenticateAsync())
            {
                return null;
            }
            response = await _httpClient.SendAsync(requestFactory());
        }

        return response;
    }

    public async Task<List<PortStatus>> GetAllPortsStatusAsync()
    {
        var ports = new List<PortStatus>();
        if (!IsConfigured)
        {
            return ports;
        }

        try
        {
            var response = await SendWithAuthAsync(() => CreateRequest(HttpMethod.Get, $"/api/s/{_config.SiteName}/stat/device"));
            if (response == null || !response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get device stats: {Status}", response?.StatusCode);
                return ports;
            }

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonNode.Parse(json);
            var data = root?["data"]?.AsArray();
            if (data == null)
            {
                return ports;
            }

            var switchMacNormalized = _config.SwitchMac.ToLowerInvariant().Replace("-", ":");
            foreach (var device in data)
            {
                var mac = device?["mac"]?.GetValue<string>()?.ToLowerInvariant();
                if (mac != switchMacNormalized)
                {
                    continue;
                }

                _deviceId = device?["_id"]?.GetValue<string>();
                var portTable = device?["port_table"]?.AsArray();
                if (portTable == null)
                {
                    continue;
                }

                foreach (var port in portTable)
                {
                    var poeMode = port?["poe_mode"]?.GetValue<string>();
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
            _logger.LogError(ex, "Error getting port statuses from UniFi");
        }

        return ports;
    }

    public async Task<bool> SetPortStateAsync(int portNumber, bool enabled)
    {
        if (!IsConfigured)
        {
            return false;
        }

        try
        {
            if (_deviceId == null)
            {
                await GetAllPortsStatusAsync();
                if (_deviceId == null)
                {
                    _logger.LogWarning("Device ID not found; cannot set port state");
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
            var json = JsonSerializer.Serialize(payload);

            var response = await SendWithAuthAsync(() =>
            {
                var req = CreateRequest(HttpMethod.Put, $"/api/s/{_config.SiteName}/rest/device/{_deviceId}");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            });

            if (response == null || !response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to set port {Port} state: {Status}", portNumber, response?.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting port {Port} state", portNumber);
            return false;
        }
    }
}
