// OBS WebSocket v5 server-side client
// Inspired by obs-web (https://github.com/Niek/obs-web) by Niek van der Maas

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace WorshipConsole.Services;

/// <summary>
/// Manages a server-side WebSocket connection to OBS Studio (OBS-WebSocket v5 protocol).
/// Credentials are read from configuration and never sent to the browser.
/// Registered as a scoped service — one instance per Blazor circuit.
/// </summary>
public sealed class ObsWebSocketService : IAsyncDisposable
{
    // ── Configuration ──
    private readonly string host;
    private readonly int port;
    private readonly string password;

    // ── WebSocket internals ──
    private ClientWebSocket? ws;
    private CancellationTokenSource? cts;
    private Task? receiveTask;
    private int requestCounter;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonNode?>> pending = new();

    // ── Connection state ──
    public bool IsConnecting { get; private set; }
    public bool IsConnected { get; private set; }
    public string? LastError { get; private set; }

    // ── OBS state ──
    public List<ObsScene> Scenes { get; private set; } = [];
    public string CurrentProgramScene { get; private set; } = string.Empty;
    public string CurrentPreviewScene { get; private set; } = string.Empty;
    public bool Streaming { get; private set; }
    public bool Recording { get; private set; }
    public bool VirtualCam { get; private set; }
    public bool ReplayBuffer { get; private set; }
    public bool StudioMode { get; private set; }
    public List<string> Profiles { get; private set; } = [];
    public string CurrentProfile { get; private set; } = string.Empty;
    public List<string> SceneCollections { get; private set; } = [];
    public string CurrentSceneCollection { get; private set; } = string.Empty;

    /// <summary>Raised on any state change so Blazor components can call StateHasChanged.</summary>
    public event Action? StateChanged;

    public ObsWebSocketService(IConfiguration configuration)
    {
        this.host = configuration["OBS:Host"] ?? "127.0.0.1";
        this.port = int.TryParse(configuration["OBS:Port"], out int p) ? p : 4455;
        this.password = configuration["OBS:Password"] ?? string.Empty;
    }

    // ── Public: connection ──

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await this.CleanupAsync();

        this.IsConnecting = true;
        this.IsConnected = false;
        this.LastError = null;
        this.NotifyStateChanged();

        this.cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        this.ws = new ClientWebSocket();

        try
        {
            await this.ws.ConnectAsync(new Uri($"ws://{this.host}:{this.port}"), this.cts.Token);
            this.receiveTask = this.ReceiveLoopAsync(this.cts.Token);
        }
        catch (Exception ex)
        {
            this.IsConnecting = false;
            this.LastError = $"Unable to connect to OBS at {this.host}:{this.port}. Ensure OBS is running and the WebSocket server is enabled. ({ex.Message})";
            this.NotifyStateChanged();
        }
    }

    public async Task DisconnectAsync()
    {
        await this.CleanupAsync();
        this.IsConnected = false;
        this.IsConnecting = false;
        this.LastError = null;
        this.NotifyStateChanged();
    }

    // ── Public: scene actions ──

    public Task SwitchSceneAsync(string name) => this.SendActionAsync("SetCurrentProgramScene", new JsonObject { ["sceneName"] = name });

    public Task SwitchPreviewSceneAsync(string name) => this.SendActionAsync("SetCurrentPreviewScene", new JsonObject { ["sceneName"] = name });

    public Task TransitionToProgramAsync() => this.SendActionAsync("TriggerStudioModeTransition");

    // ── Public: output actions ──

    public Task StartStreamAsync() => this.SendActionAsync("StartStream");
    public Task StopStreamAsync() => this.SendActionAsync("StopStream");
    public Task StartRecordAsync() => this.SendActionAsync("StartRecord");
    public Task StopRecordAsync() => this.SendActionAsync("StopRecord");
    public Task StartVirtualCamAsync() => this.SendActionAsync("StartVirtualCam");
    public Task StopVirtualCamAsync() => this.SendActionAsync("StopVirtualCam");
    public Task StartReplayBufferAsync() => this.SendActionAsync("StartReplayBuffer");
    public Task StopReplayBufferAsync() => this.SendActionAsync("StopReplayBuffer");
    public Task SaveReplayBufferAsync() => this.SendActionAsync("SaveReplayBuffer");

    // ── Public: settings actions ──

    public Task SetStudioModeAsync(bool enabled) => this.SendActionAsync("SetStudioModeEnabled", new JsonObject { ["studioModeEnabled"] = enabled });

    public Task SetProfileAsync(string name) => this.SendActionAsync("SetCurrentProfile", new JsonObject { ["profileName"] = name });

    public Task SetSceneCollectionAsync(string name) => this.SendActionAsync("SetCurrentSceneCollection", new JsonObject { ["sceneCollectionName"] = name });

    // ── Receive loop ──

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[64 * 1024];
        StringBuilder sb = new();
        bool closedByServer = false;

        try
        {
            while (!ct.IsCancellationRequested && this.ws?.State == WebSocketState.Open)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await this.ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        closedByServer = true;
                        break;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                if (closedByServer) break;

                JsonNode? json = JsonNode.Parse(sb.ToString());
                if (json is null) continue;

                int op = json["op"]?.GetValue<int>() ?? -1;
                JsonNode? data = json["d"];

                switch (op)
                {
                    case 0: await this.HandleHelloAsync(data, ct); break;
                    case 2: await this.FetchInitialStateAsync(ct); break;
                    case 5:
                        this.HandleEvent(data); break;
                    case 7:
                        this.HandleResponse(data); break;
                }
            }
        }
        catch (OperationCanceledException) { /* expected on disconnect */ }
        catch (Exception ex)
        {
            this.LastError = $"OBS connection lost: {ex.Message}";
        }

        if (this.IsConnected || this.IsConnecting)
        {
            this.IsConnected = false;
            this.IsConnecting = false;
            this.LastError ??= "Connection closed.";
            this.NotifyStateChanged();
        }
    }

    // ── Protocol: Hello (op:0) ──

    private async Task HandleHelloAsync(JsonNode? data, CancellationToken ct)
    {
        string? authResponse = null;
        JsonNode? authentication = data?["authentication"];

        if (authentication is not null)
        {
            if (string.IsNullOrEmpty(this.password))
            {
                this.IsConnecting = false;
                this.LastError = "OBS WebSocket requires a password. Please set OBS:Password in appsettings.json.";
                this.NotifyStateChanged();
                try { await this.ws!.CloseAsync(WebSocketCloseStatus.NormalClosure, "No password configured", ct); }
                catch (WebSocketException) { /* socket may already be closed */ }
                catch (OperationCanceledException) { /* cancellation during close is fine */ }
                return;
            }

            string challenge = authentication["challenge"]!.GetValue<string>();
            string salt = authentication["salt"]!.GetValue<string>();
            authResponse = ComputeAuthResponse(this.password, salt, challenge);
        }

        JsonObject identify = new()
        {
            ["op"] = 1,
            ["d"] = new JsonObject
            {
                ["rpcVersion"] = 1,
                ["authentication"] = authResponse,
                // EventSubscriptions: General(1) | Config(2) | Scenes(4) | Outputs(64) = 71
                ["eventSubscriptions"] = 71
            }
        };

        await this.SendRawAsync(identify, ct);
    }

    private static string ComputeAuthResponse(string password, string salt, string challenge)
    {
        byte[] secretBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
        string secret = Convert.ToBase64String(secretBytes);
        byte[] authBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge));
        return Convert.ToBase64String(authBytes);
    }

    // ── Protocol: Identified (op:2) — fetch state ──

    private async Task FetchInitialStateAsync(CancellationToken ct)
    {
        try
        {
            JsonNode?[] results = await Task.WhenAll(this.SendRequestAsync("GetSceneList", null, ct), this.SendRequestAsync("GetStreamStatus", null, ct), this.SendRequestAsync("GetRecordStatus", null, ct), this.SendRequestAsync("GetVirtualCamStatus", null, ct), this.SendRequestAsync("GetStudioModeEnabled", null, ct), this.SendRequestAsync("GetProfileList", null, ct), this.SendRequestAsync("GetSceneCollectionList", null, ct)
            );

            JsonNode? replayStatus = null;
            try { replayStatus = await this.SendRequestAsync("GetReplayBufferStatus", null, ct); } catch { /* not configured */ }

            this.Scenes = ParseScenes(results[0]?["scenes"]);
            this.CurrentProgramScene = results[0]?["currentProgramSceneName"]?.GetValue<string>() ?? string.Empty;
            this.CurrentPreviewScene = results[0]?["currentPreviewSceneName"]?.GetValue<string>() ?? string.Empty;
            this.Streaming = results[1]?["outputActive"]?.GetValue<bool>() ?? false;
            this.Recording = results[2]?["outputActive"]?.GetValue<bool>() ?? false;
            this.VirtualCam = results[3]?["outputActive"]?.GetValue<bool>() ?? false;
            this.StudioMode = results[4]?["studioModeEnabled"]?.GetValue<bool>() ?? false;
            this.Profiles = ParseStringList(results[5]?["profiles"]);
            this.CurrentProfile = results[5]?["currentProfileName"]?.GetValue<string>() ?? string.Empty;
            this.SceneCollections = ParseStringList(results[6]?["sceneCollections"]);
            this.CurrentSceneCollection = results[6]?["currentSceneCollectionName"]?.GetValue<string>() ?? string.Empty;
            this.ReplayBuffer = replayStatus?["outputActive"]?.GetValue<bool>() ?? false;

            this.IsConnecting = false;
            this.IsConnected = true;
            this.LastError = null;
            this.NotifyStateChanged();
        }
        catch (Exception ex)
        {
            this.IsConnecting = false;
            this.IsConnected = false;
            this.LastError = $"Failed to load OBS state: {ex.Message}";
            this.NotifyStateChanged();
        }
    }

    // ── Protocol: Event (op:5) ──

    private void HandleEvent(JsonNode? data)
    {
        string? eventType = data?["eventType"]?.GetValue<string>();
        JsonNode? eventData = data?["eventData"];

        switch (eventType)
        {
            case "CurrentProgramSceneChanged":
                this.CurrentProgramScene = eventData?["sceneName"]?.GetValue<string>() ?? string.Empty;
                this.NotifyStateChanged();
                break;
            case "CurrentPreviewSceneChanged":
                this.CurrentPreviewScene = eventData?["sceneName"]?.GetValue<string>() ?? string.Empty;
                this.NotifyStateChanged();
                break;
            case "StreamStateChanged":
                this.Streaming = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                this.NotifyStateChanged();
                break;
            case "RecordStateChanged":
                this.Recording = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                this.NotifyStateChanged();
                break;
            case "VirtualcamStateChanged":
                this.VirtualCam = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                this.NotifyStateChanged();
                break;
            case "ReplayBufferStateChanged":
                this.ReplayBuffer = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                this.NotifyStateChanged();
                break;
            case "StudioModeStateChanged":
                this.StudioMode = eventData?["studioModeEnabled"]?.GetValue<bool>() ?? false;
                this.NotifyStateChanged();
                break;
            case "SceneListChanged":
                this.Scenes = ParseScenes(eventData?["scenes"]);
                this.NotifyStateChanged();
                break;
            case "CurrentProfileChanged":
                this.CurrentProfile = eventData?["profileName"]?.GetValue<string>() ?? string.Empty;
                this.NotifyStateChanged();
                break;
            case "CurrentSceneCollectionChanged":
                this.CurrentSceneCollection = eventData?["sceneCollectionName"]?.GetValue<string>() ?? string.Empty;
                this.NotifyStateChanged();
                break;
        }
    }

    // ── Protocol: Response (op:7) ──

    private void HandleResponse(JsonNode? data)
    {
        string? requestId = data?["requestId"]?.GetValue<string>();
        if (requestId is null || !this.pending.TryRemove(requestId, out TaskCompletionSource<JsonNode?>? tcs)) return;

        JsonNode? status = data?["requestStatus"];
        if (status?["result"]?.GetValue<bool>() == true)
        {
            tcs.TrySetResult(data?["responseData"]);
        }
        else
        {
            int? code = status?["code"]?.GetValue<int>();
            string? comment = status?["comment"]?.GetValue<string>() ?? data?["requestType"]?.GetValue<string>();
            tcs.TrySetException(new InvalidOperationException($"OBS request failed ({code}): {comment}"));
        }
    }

    // ── Send helpers ──

    private async Task SendRawAsync(JsonNode msg, CancellationToken ct)
    {
        if (this.ws?.State != WebSocketState.Open) return;
        byte[] bytes = Encoding.UTF8.GetBytes(msg.ToJsonString());
        await this.ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private async Task<JsonNode?> SendRequestAsync(string requestType, JsonNode? requestData, CancellationToken ct)
    {
        if (this.ws?.State != WebSocketState.Open)
            throw new InvalidOperationException("OBS WebSocket is not connected.");

        string id = Interlocked.Increment(ref this.requestCounter).ToString();
        TaskCompletionSource<JsonNode?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        this.pending[id] = tcs;

        JsonObject msg = new()
        {
            ["op"] = 6,
            ["d"] = new JsonObject
            {
                ["requestType"] = requestType,
                ["requestId"] = id,
                ["requestData"] = requestData
            }
        };

        await this.SendRawAsync(msg, ct);

        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(10));
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        await using CancellationTokenRegistration reg = linked.Token.Register(() =>
        {
            if (this.pending.TryRemove(id, out TaskCompletionSource<JsonNode?>? pendingTcs))
            {
                pendingTcs.TrySetCanceled(linked.Token);
            }
        });

        return await tcs.Task;
    }

    private async Task SendActionAsync(string requestType, JsonNode? requestData = null)
    {
        if (!this.IsConnected) return;
        try
        {
            await this.SendRequestAsync(requestType, requestData, this.cts?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            this.LastError = $"OBS action failed ({requestType}): {ex.Message}";
            this.NotifyStateChanged();
        }
    }

    // ── Helpers ──

    private static List<ObsScene> ParseScenes(JsonNode? node)
    {
        if (node is not JsonArray arr) return [];
        return arr
            .Select(s => new ObsScene { SceneName = s?["sceneName"]?.GetValue<string>() ?? string.Empty })
            .Where(s => !string.IsNullOrEmpty(s.SceneName) && !s.SceneName.Contains("(hidden)"))
            .Reverse()
            .ToList();
    }

    private static List<string> ParseStringList(JsonNode? node)
    {
        if (node is not JsonArray arr) return [];
        return arr
            .Select(s => s?.GetValue<string>() ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    private void NotifyStateChanged() => this.StateChanged?.Invoke();

    private async Task CleanupAsync()
    {
        this.cts?.Cancel();
        if (this.receiveTask is not null)
        {
            try { await this.receiveTask.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (TimeoutException) { /* receive task did not exit in time; it will be abandoned */ }
            catch (OperationCanceledException) { /* expected */ }
        }
        this.receiveTask = null;

        if (this.ws is not null)
        {
            try
            {
                if (this.ws.State == WebSocketState.Open)
                    await this.ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", CancellationToken.None);
            }
            catch (WebSocketException) { /* socket may already be closed or faulted */ }
            this.ws.Dispose();
            this.ws = null;
        }

        this.cts?.Dispose();
        this.cts = null;

        foreach ((string _, TaskCompletionSource<JsonNode?> tcs) in this.pending)
            tcs.TrySetCanceled();
        this.pending.Clear();
    }

    public async ValueTask DisposeAsync() => await this.CleanupAsync();
}

/// <summary>Lightweight OBS scene descriptor.</summary>
public sealed record ObsScene
{
    public string SceneName { get; init; } = string.Empty;
}
