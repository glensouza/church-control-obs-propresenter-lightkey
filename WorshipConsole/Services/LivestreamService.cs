using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WorshipConsole.Services;

public class LivestreamService(HttpClient httpClient, SettingsService settingsService, IConfiguration configuration)
{
    public async Task<(bool success, string message, string? videoId)> ScheduleYouTubeAsync(string title, string description, DateTime startTime, byte[]? thumbnailBytes)
    {
        try
        {
            string? clientId = configuration["YouTube:ClientId"];
            string? clientSecret = configuration["YouTube:ClientSecret"];
            string? refreshToken = configuration["YouTube:RefreshToken"];
            string streamId = await settingsService.GetSettingAsync("Livestream", "YouTubeStreamId");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
            {
                return (false, "YouTube API credentials missing in appsettings.json.", null);
            }

            if (string.IsNullOrEmpty(streamId))
            {
                return (false, "YouTube Stream ID missing in Administration settings.", null);
            }

            string accessToken = await this.GetYouTubeAccessTokenAsync(clientId, clientSecret, refreshToken);
            
            // 1. Create Broadcast
            string broadcastId = await this.CreateYouTubeBroadcastAsync(accessToken, title, description, startTime);
            
            // 2. Bind to Stream
            await this.BindYouTubeBroadcastAsync(accessToken, broadcastId, streamId);

            // 3. Upload Thumbnail (Optional)
            if (thumbnailBytes != null)
            {
                await this.UploadYouTubeThumbnailAsync(accessToken, broadcastId, thumbnailBytes);
            }

            return (true, "YouTube scheduled!", broadcastId);
        }
        catch (Exception ex)
        {
            return (false, $"YouTube Error: {ex.Message}", null);
        }
    }

    public async Task<(bool success, string message)> ScheduleFacebookAsync(string title, string description, DateTime startTime, byte[]? thumbnailBytes)
    {
        try
        {
            // Facebook authentication uses a non-expiring System User Token generated via Meta Business Manager.
            // To regenerate: business.facebook.com → Settings → Users → System Users → Worship Console Bot → Generate New Token.
            // Required permissions: pages_manage_posts, pages_read_engagement.
            string? configuredToken = configuration["Facebook:PageAccessToken"]?.Trim();
            string graphApiVersion = (configuration["Facebook:GraphApiVersion"] ?? "v22.0").Trim();
            string pageId = (await settingsService.GetSettingAsync("Livestream", "FacebookPageId")).Trim();

            if (string.IsNullOrEmpty(configuredToken))
            {
                return (false, "Facebook System User Token missing in appsettings.json.");
            }

            if (string.IsNullOrEmpty(pageId))
            {
                return (false, "Facebook Page ID missing in Administration settings.");
            }

            (bool preflightSuccess, string preflightMessage, string pageAccessToken) = await this.ValidateFacebookPreflightAsync(graphApiVersion, pageId, configuredToken);
            if (!preflightSuccess)
            {
                return (false, preflightMessage);
            }

            string url = $"https://graph.facebook.com/{graphApiVersion}/{pageId}/live_videos";
            
            using MultipartFormDataContent content = new();
            long unixTimestamp = new DateTimeOffset(startTime).ToUnixTimeSeconds();

            content.Add(new StringContent("SCHEDULED_UNPUBLISHED"), "status");
            content.Add(new StringContent(unixTimestamp.ToString()), "planned_start_time");
            content.Add(new StringContent(title), "title");
            content.Add(new StringContent(description), "description");
            content.Add(new StringContent(pageAccessToken), "access_token");

            // event_params triggers the creation of a Page Event and enables auto-start
            var eventParams = new { scheduled_start_time = unixTimestamp };
            content.Add(new StringContent(JsonSerializer.Serialize(eventParams)), "event_params");

            if (thumbnailBytes != null)
            {
                ByteArrayContent imageContent = new(thumbnailBytes);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                content.Add(imageContent, "schedule_custom_profile_image", "thumbnail.jpg");
            }

            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            string responseString = await response.Content.ReadAsStringAsync();

            return !response.IsSuccessStatusCode
                ? (false, this.ExtractFacebookErrorMessage(responseString, "Facebook API Error"))
                : (true, "Facebook scheduled successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Facebook Error: {ex.Message}");
        }
    }

    private async Task<(bool success, string message, string pageAccessToken)> ValidateFacebookPreflightAsync(string graphApiVersion, string pageId, string configuredToken)
    {
        string pageInfoUrl = $"https://graph.facebook.com/{graphApiVersion}/{pageId}?fields=id,name,access_token&access_token={Uri.EscapeDataString(configuredToken)}";
        HttpResponseMessage pageInfoResponse = await httpClient.GetAsync(pageInfoUrl);
        string pageInfoResponseString = await pageInfoResponse.Content.ReadAsStringAsync();

        if (!pageInfoResponse.IsSuccessStatusCode)
        {
            return (false, this.ExtractFacebookErrorMessage(pageInfoResponseString, "Facebook preflight failed while validating page access"), string.Empty);
        }

        using JsonDocument pageDoc = JsonDocument.Parse(pageInfoResponseString);
        if (!pageDoc.RootElement.TryGetProperty("id", out JsonElement idElement))
        {
            return (false, "Facebook preflight failed: page lookup did not return a valid page id.", string.Empty);
        }

        string resolvedPageId = idElement.GetString() ?? string.Empty;
        if (!string.Equals(resolvedPageId, pageId, StringComparison.Ordinal))
        {
            return (false, $"Facebook preflight failed: configured page id '{pageId}' does not match resolved page id '{resolvedPageId}'.", string.Empty);
        }

        string pageAccessToken = configuredToken;
        if (pageDoc.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
        {
            string? resolvedPageToken = tokenElement.GetString();
            if (!string.IsNullOrWhiteSpace(resolvedPageToken))
            {
                pageAccessToken = resolvedPageToken;
            }
        }

        string liveVideosEdgeUrl = $"https://graph.facebook.com/{graphApiVersion}/{pageId}/live_videos?limit=1&access_token={Uri.EscapeDataString(pageAccessToken)}";
        HttpResponseMessage liveVideosEdgeResponse = await httpClient.GetAsync(liveVideosEdgeUrl);
        string liveVideosEdgeResponseString = await liveVideosEdgeResponse.Content.ReadAsStringAsync();
        if (!liveVideosEdgeResponse.IsSuccessStatusCode)
        {
            return (false, this.ExtractFacebookErrorMessage(liveVideosEdgeResponseString, "Facebook preflight failed while validating live video permissions"), string.Empty);
        }

        return (true, "Facebook preflight succeeded.", pageAccessToken);
    }

    private string ExtractFacebookErrorMessage(string responseString, string prefix)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseString);
            if (!doc.RootElement.TryGetProperty("error", out JsonElement errorElement))
            {
                return $"{prefix}: {responseString}";
            }

            string message = errorElement.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString() ?? "Unknown Facebook error"
                : "Unknown Facebook error";

            string? code = errorElement.TryGetProperty("code", out JsonElement codeElement)
                ? codeElement.GetRawText()
                : null;

            return string.IsNullOrEmpty(code)
                ? $"{prefix}: {message}"
                : $"{prefix} (code {code}): {message}";
        }
        catch (JsonException)
        {
            return $"{prefix}: {responseString}";
        }
    }

    private async Task<string> GetYouTubeAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "https://oauth2.googleapis.com/token");
        FormUrlEncodedContent payload = new([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("grant_type", "refresh_token")
        ]);
        request.Content = payload;

        HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("access_token").GetString() ?? throw new Exception("Failed to get YouTube access token.");
    }

    private async Task<string> CreateYouTubeBroadcastAsync(string accessToken, string title, string description, DateTime startTime)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "https://www.googleapis.com/youtube/v3/liveBroadcasts?part=snippet,status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var payload = new
        {
            snippet = new
            {
                title,
                description,
                scheduledStartTime = startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            status = new
            {
                privacyStatus = "public",
                selfDeclaredMadeForKids = false
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await httpClient.SendAsync(request);
        
        string responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"YouTube Broadcast Create Error: {responseString}");
        }

        JsonDocument doc = JsonDocument.Parse(responseString);
        return doc.RootElement.GetProperty("id").GetString() ?? throw new Exception("Failed to get Broadcast ID.");
    }

    private async Task BindYouTubeBroadcastAsync(string accessToken, string broadcastId, string streamId)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"https://www.googleapis.com/youtube/v3/liveBroadcasts/bind?id={broadcastId}&part=id&streamId={streamId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            string responseString = await response.Content.ReadAsStringAsync();
            throw new Exception($"YouTube Bind Error: {responseString}");
        }
    }

    private async Task UploadYouTubeThumbnailAsync(string accessToken, string videoId, byte[] imageBytes)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"https://www.googleapis.com/upload/youtube/v3/thumbnails/set?videoId={videoId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        ByteArrayContent content = new(imageBytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        request.Content = content;

        HttpResponseMessage response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            string responseString = await response.Content.ReadAsStringAsync();
            throw new Exception($"YouTube Thumbnail Upload Error: {responseString}");
        }
    }
}
