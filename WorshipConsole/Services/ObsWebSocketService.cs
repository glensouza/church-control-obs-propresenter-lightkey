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
    private readonly string _host;
    private readonly int _port;
    private readonly string _password;

    // ── WebSocket internals ──
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private int _requestCounter;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonNode?>> _pending = new();

    // ── Connection state ──
    public bool IsConnecting { get; private set; }
    public bool IsConnected { get; private set; }
    public string? LastError { get; private set; }

    // ── OBS state ──
    public List<OBSScene> Scenes { get; private set; } = [];
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
        _host = configuration["OBS:Host"] ?? "127.0.0.1";
        _port = int.TryParse(configuration["OBS:Port"], out var p) ? p : 4455;
        _password = configuration["OBS:Password"] ?? string.Empty;
    }

    // ── Public: connection ──

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await CleanupAsync();

        IsConnecting = true;
        IsConnected = false;
        LastError = null;
        NotifyStateChanged();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ws = new ClientWebSocket();

        try
        {
            await _ws.ConnectAsync(new Uri($"ws://{_host}:{_port}"), _cts.Token);
            _receiveTask = ReceiveLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            LastError = $"Unable to connect to OBS at {_host}:{_port}. Ensure OBS is running and the WebSocket server is enabled. ({ex.Message})";
            NotifyStateChanged();
        }
    }

    public async Task DisconnectAsync()
    {
        await CleanupAsync();
        IsConnected = false;
        IsConnecting = false;
        LastError = null;
        NotifyStateChanged();
    }

    // ── Public: scene actions ──

    public Task SwitchSceneAsync(string name) =>
        SendActionAsync("SetCurrentProgramScene", new JsonObject { ["sceneName"] = name });

    public Task SwitchPreviewSceneAsync(string name) =>
        SendActionAsync("SetCurrentPreviewScene", new JsonObject { ["sceneName"] = name });

    public Task TransitionToProgramAsync() =>
        SendActionAsync("TriggerStudioModeTransition");

    // ── Public: output actions ──

    public Task StartStreamAsync() => SendActionAsync("StartStream");
    public Task StopStreamAsync() => SendActionAsync("StopStream");
    public Task StartRecordAsync() => SendActionAsync("StartRecord");
    public Task StopRecordAsync() => SendActionAsync("StopRecord");
    public Task StartVirtualCamAsync() => SendActionAsync("StartVirtualCam");
    public Task StopVirtualCamAsync() => SendActionAsync("StopVirtualCam");
    public Task StartReplayBufferAsync() => SendActionAsync("StartReplayBuffer");
    public Task StopReplayBufferAsync() => SendActionAsync("StopReplayBuffer");
    public Task SaveReplayBufferAsync() => SendActionAsync("SaveReplayBuffer");

    // ── Public: settings actions ──

    public Task SetStudioModeAsync(bool enabled) =>
        SendActionAsync("SetStudioModeEnabled", new JsonObject { ["studioModeEnabled"] = enabled });

    public Task SetProfileAsync(string name) =>
        SendActionAsync("SetCurrentProfile", new JsonObject { ["profileName"] = name });

    public Task SetSceneCollectionAsync(string name) =>
        SendActionAsync("SetCurrentSceneCollection", new JsonObject { ["sceneCollectionName"] = name });

    // ── Receive loop ──

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var sb = new StringBuilder();
        var closedByServer = false;

        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        closedByServer = true;
                        break;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                if (closedByServer) break;

                var json = JsonNode.Parse(sb.ToString());
                if (json is null) continue;

                var op = json["op"]?.GetValue<int>() ?? -1;
                var data = json["d"];

                switch (op)
                {
                    case 0: await HandleHelloAsync(data, ct); break;
                    case 2: await FetchInitialStateAsync(ct); break;
                    case 5: HandleEvent(data); break;
                    case 7: HandleResponse(data); break;
                }
            }
        }
        catch (OperationCanceledException) { /* expected on disconnect */ }
        catch (Exception ex)
        {
            LastError = $"OBS connection lost: {ex.Message}";
        }

        if (IsConnected || IsConnecting)
        {
            IsConnected = false;
            IsConnecting = false;
            LastError ??= "Connection closed.";
            NotifyStateChanged();
        }
    }

    // ── Protocol: Hello (op:0) ──

    private async Task HandleHelloAsync(JsonNode? data, CancellationToken ct)
    {
        string? authResponse = null;
        var authentication = data?["authentication"];

        if (authentication is not null)
        {
            if (string.IsNullOrEmpty(_password))
            {
                IsConnecting = false;
                LastError = "OBS WebSocket requires a password. Please set OBS:Password in appsettings.json.";
                NotifyStateChanged();
                try { await _ws!.CloseAsync(WebSocketCloseStatus.NormalClosure, "No password configured", ct); }
                catch (WebSocketException) { /* socket may already be closed */ }
                catch (OperationCanceledException) { /* cancellation during close is fine */ }
                return;
            }

            var challenge = authentication["challenge"]!.GetValue<string>();
            var salt = authentication["salt"]!.GetValue<string>();
            authResponse = ComputeAuthResponse(_password, salt, challenge);
        }

        var identify = new JsonObject
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

        await SendRawAsync(identify, ct);
    }

    private static string ComputeAuthResponse(string password, string salt, string challenge)
    {
        var secretBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
        var secret = Convert.ToBase64String(secretBytes);
        var authBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge));
        return Convert.ToBase64String(authBytes);
    }

    // ── Protocol: Identified (op:2) — fetch state ──

    private async Task FetchInitialStateAsync(CancellationToken ct)
    {
        try
        {
            var results = await Task.WhenAll(
                SendRequestAsync("GetSceneList", null, ct),
                SendRequestAsync("GetStreamStatus", null, ct),
                SendRequestAsync("GetRecordStatus", null, ct),
                SendRequestAsync("GetVirtualCamStatus", null, ct),
                SendRequestAsync("GetStudioModeEnabled", null, ct),
                SendRequestAsync("GetProfileList", null, ct),
                SendRequestAsync("GetSceneCollectionList", null, ct)
            );

            JsonNode? replayStatus = null;
            try { replayStatus = await SendRequestAsync("GetReplayBufferStatus", null, ct); } catch { /* not configured */ }

            Scenes = ParseScenes(results[0]?["scenes"]);
            CurrentProgramScene = results[0]?["currentProgramSceneName"]?.GetValue<string>() ?? string.Empty;
            CurrentPreviewScene = results[0]?["currentPreviewSceneName"]?.GetValue<string>() ?? string.Empty;
            Streaming = results[1]?["outputActive"]?.GetValue<bool>() ?? false;
            Recording = results[2]?["outputActive"]?.GetValue<bool>() ?? false;
            VirtualCam = results[3]?["outputActive"]?.GetValue<bool>() ?? false;
            StudioMode = results[4]?["studioModeEnabled"]?.GetValue<bool>() ?? false;
            Profiles = ParseStringList(results[5]?["profiles"]);
            CurrentProfile = results[5]?["currentProfileName"]?.GetValue<string>() ?? string.Empty;
            SceneCollections = ParseStringList(results[6]?["sceneCollections"]);
            CurrentSceneCollection = results[6]?["currentSceneCollectionName"]?.GetValue<string>() ?? string.Empty;
            ReplayBuffer = replayStatus?["outputActive"]?.GetValue<bool>() ?? false;

            IsConnecting = false;
            IsConnected = true;
            LastError = null;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            IsConnected = false;
            LastError = $"Failed to load OBS state: {ex.Message}";
            NotifyStateChanged();
        }
    }

    // ── Protocol: Event (op:5) ──

    private void HandleEvent(JsonNode? data)
    {
        var eventType = data?["eventType"]?.GetValue<string>();
        var eventData = data?["eventData"];

        switch (eventType)
        {
            case "CurrentProgramSceneChanged":
                CurrentProgramScene = eventData?["sceneName"]?.GetValue<string>() ?? string.Empty;
                NotifyStateChanged();
                break;
            case "CurrentPreviewSceneChanged":
                CurrentPreviewScene = eventData?["sceneName"]?.GetValue<string>() ?? string.Empty;
                NotifyStateChanged();
                break;
            case "StreamStateChanged":
                Streaming = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                NotifyStateChanged();
                break;
            case "RecordStateChanged":
                Recording = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                NotifyStateChanged();
                break;
            case "VirtualcamStateChanged":
                VirtualCam = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                NotifyStateChanged();
                break;
            case "ReplayBufferStateChanged":
                ReplayBuffer = eventData?["outputActive"]?.GetValue<bool>() ?? false;
                NotifyStateChanged();
                break;
            case "StudioModeStateChanged":
                StudioMode = eventData?["studioModeEnabled"]?.GetValue<bool>() ?? false;
                NotifyStateChanged();
                break;
            case "SceneListChanged":
                Scenes = ParseScenes(eventData?["scenes"]);
                NotifyStateChanged();
                break;
            case "CurrentProfileChanged":
                CurrentProfile = eventData?["profileName"]?.GetValue<string>() ?? string.Empty;
                NotifyStateChanged();
                break;
            case "CurrentSceneCollectionChanged":
                CurrentSceneCollection = eventData?["sceneCollectionName"]?.GetValue<string>() ?? string.Empty;
                NotifyStateChanged();
                break;
        }
    }

    // ── Protocol: Response (op:7) ──

    private void HandleResponse(JsonNode? data)
    {
        var requestId = data?["requestId"]?.GetValue<string>();
        if (requestId is null || !_pending.TryRemove(requestId, out var tcs)) return;

        var status = data?["requestStatus"];
        if (status?["result"]?.GetValue<bool>() == true)
        {
            tcs.TrySetResult(data?["responseData"]);
        }
        else
        {
            var code = status?["code"]?.GetValue<int>();
            var comment = status?["comment"]?.GetValue<string>() ?? data?["requestType"]?.GetValue<string>();
            tcs.TrySetException(new InvalidOperationException($"OBS request failed ({code}): {comment}"));
        }
    }

    // ── Send helpers ──

    private async Task SendRawAsync(JsonNode msg, CancellationToken ct)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(msg.ToJsonString());
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private async Task<JsonNode?> SendRequestAsync(string requestType, JsonNode? requestData, CancellationToken ct)
    {
        if (_ws?.State != WebSocketState.Open)
            throw new InvalidOperationException("OBS WebSocket is not connected.");

        var id = Interlocked.Increment(ref _requestCounter).ToString();
        var tcs = new TaskCompletionSource<JsonNode?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;

        var msg = new JsonObject
        {
            ["op"] = 6,
            ["d"] = new JsonObject
            {
                ["requestType"] = requestType,
                ["requestId"] = id,
                ["requestData"] = requestData
            }
        };

        await SendRawAsync(msg, ct);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        using var reg = linked.Token.Register(() =>
        {
            if (_pending.TryRemove(id, out var pendingTcs))
                pendingTcs.TrySetCanceled(linked.Token);
        });

        return await tcs.Task;
    }

    private async Task SendActionAsync(string requestType, JsonNode? requestData = null)
    {
        if (!IsConnected) return;
        try
        {
            await SendRequestAsync(requestType, requestData, _cts?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            LastError = $"OBS action failed ({requestType}): {ex.Message}";
            NotifyStateChanged();
        }
    }

    // ── Helpers ──

    private static List<OBSScene> ParseScenes(JsonNode? node)
    {
        if (node is not JsonArray arr) return [];
        return arr
            .Select(s => new OBSScene { SceneName = s?["sceneName"]?.GetValue<string>() ?? string.Empty })
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

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private async Task CleanupAsync()
    {
        _cts?.Cancel();
        if (_receiveTask is not null)
        {
            try { await _receiveTask.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (TimeoutException) { /* receive task did not exit in time; it will be abandoned */ }
            catch (OperationCanceledException) { /* expected */ }
        }
        _receiveTask = null;

        if (_ws is not null)
        {
            try
            {
                if (_ws.State == WebSocketState.Open)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", CancellationToken.None);
            }
            catch (WebSocketException) { /* socket may already be closed or faulted */ }
            _ws.Dispose();
            _ws = null;
        }

        _cts?.Dispose();
        _cts = null;

        foreach (var (_, tcs) in _pending)
            tcs.TrySetCanceled();
        _pending.Clear();
    }

    public async ValueTask DisposeAsync() => await CleanupAsync();
}

/// <summary>Lightweight OBS scene descriptor.</summary>
public sealed record OBSScene
{
    public string SceneName { get; init; } = string.Empty;
}
