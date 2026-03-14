// OBS WebSocket v5 Protocol Client
// Inspired by obs-web (https://github.com/Niek/obs-web) by Niek van der Maas

let ws = null;
let dotNetHelper = null;
let pendingRequests = new Map();
let requestCounter = 0;
let _password = '';
let _connectionError = false; // set on onerror so onclose doesn't overwrite Error state

// --- Crypto helpers ---

async function sha256base64(str) {
    const data = new TextEncoder().encode(str);
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    const hashArray = new Uint8Array(hashBuffer);
    let binary = '';
    for (let i = 0; i < hashArray.length; i++) {
        binary += String.fromCharCode(hashArray[i]);
    }
    return btoa(binary);
}

// --- Low-level send helpers ---

function sendRaw(op, data) {
    if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({ op, d: data }));
    }
}

function sendRequest(type, requestData) {
    return new Promise((resolve, reject) => {
        if (!ws || ws.readyState !== WebSocket.OPEN) {
            reject(new Error('WebSocket not open'));
            return;
        }

        const id = String(++requestCounter);
        const timeoutId = setTimeout(() => {
            if (pendingRequests.has(id)) {
                pendingRequests.delete(id);
                reject(new Error(`Request timed out: ${type}`));
            }
        }, 10000);

        pendingRequests.set(id, { resolve, reject, timeoutId });

        const msg = { op: 6, d: { requestType: type, requestId: id } };
        if (requestData !== undefined) {
            msg.d.requestData = requestData;
        }
        ws.send(JSON.stringify(msg));
    });
}

// --- Protocol message handlers ---

async function handleMessage(msg) {
    switch (msg.op) {
        case 0: await handleHello(msg.d); break;
        case 2: await fetchInitialState(); break;
        case 5: handleEvent(msg.d); break;
        case 7: handleRequestResponse(msg.d); break;
    }
}

async function handleHello(data) {
    let auth;
    if (data.authentication) {
        if (!_password) {
            dotNetHelper?.invokeMethodAsync('OnConnectionError', 'OBS WebSocket requires a password. Please set it in appsettings.json.');
            ws.close();
            return;
        }
        const { challenge, salt } = data.authentication;
        const secret = await sha256base64(_password + salt);
        auth = await sha256base64(secret + challenge);
    }

    // EventSubscriptions: General(1) | Config(2) | Scenes(4) | Outputs(64) = 71
    sendRaw(1, { rpcVersion: 1, authentication: auth, eventSubscriptions: 71 });
}

async function fetchInitialState() {
    try {
        const [sceneList, streamStatus, recordStatus, vcamStatus, studioMode, profileList, sceneCollList] = await Promise.all([
            sendRequest('GetSceneList'),
            sendRequest('GetStreamStatus'),
            sendRequest('GetRecordStatus'),
            sendRequest('GetVirtualCamStatus'),
            sendRequest('GetStudioModeEnabled'),
            sendRequest('GetProfileList'),
            sendRequest('GetSceneCollectionList')
        ]);

        let replayStatus = { outputActive: false };
        try { replayStatus = await sendRequest('GetReplayBufferStatus'); } catch { /* replay buffer may not be configured */ }

        const state = {
            scenes: filterAndSortScenes(sceneList.scenes || []),
            currentProgramSceneName: sceneList.currentProgramSceneName || '',
            currentPreviewSceneName: sceneList.currentPreviewSceneName || '',
            streaming: streamStatus.outputActive || false,
            recording: recordStatus.outputActive || false,
            virtualCam: vcamStatus.outputActive || false,
            replayBuffer: replayStatus.outputActive || false,
            studioMode: studioMode.studioModeEnabled || false,
            profiles: profileList.profiles || [],
            currentProfile: profileList.currentProfileName || '',
            sceneCollections: sceneCollList.sceneCollections || [],
            currentSceneCollection: sceneCollList.currentSceneCollectionName || ''
        };

        await dotNetHelper?.invokeMethodAsync('OnConnected', JSON.stringify(state));
    } catch (err) {
        dotNetHelper?.invokeMethodAsync('OnConnectionError', `Failed to load OBS state: ${err.message}`);
    }
}

function filterAndSortScenes(scenes) {
    return [...scenes]
        .reverse()
        .filter(s => !s.sceneName.includes('(hidden)'));
}

function handleRequestResponse(data) {
    const pending = pendingRequests.get(data.requestId);
    if (!pending) return;

    clearTimeout(pending.timeoutId);
    pendingRequests.delete(data.requestId);

    if (data.requestStatus?.result) {
        pending.resolve(data.responseData || {});
    } else {
        pending.reject(new Error(`OBS request failed (${data.requestStatus?.code}): ${data.requestStatus?.comment || data.requestType}`));
    }
}

function handleEvent(data) {
    switch (data.eventType) {
        case 'CurrentProgramSceneChanged':
            dotNetHelper?.invokeMethodAsync('OnSceneChanged', data.eventData.sceneName, false);
            break;
        case 'CurrentPreviewSceneChanged':
            dotNetHelper?.invokeMethodAsync('OnSceneChanged', data.eventData.sceneName, true);
            break;
        case 'StreamStateChanged':
            dotNetHelper?.invokeMethodAsync('OnStreamStateChanged', data.eventData.outputActive);
            break;
        case 'RecordStateChanged':
            dotNetHelper?.invokeMethodAsync('OnRecordStateChanged', data.eventData.outputActive);
            break;
        case 'VirtualcamStateChanged':
            dotNetHelper?.invokeMethodAsync('OnVirtualCamStateChanged', data.eventData.outputActive);
            break;
        case 'ReplayBufferStateChanged':
            dotNetHelper?.invokeMethodAsync('OnReplayBufferStateChanged', data.eventData.outputActive);
            break;
        case 'StudioModeStateChanged':
            dotNetHelper?.invokeMethodAsync('OnStudioModeChanged', data.eventData.studioModeEnabled);
            break;
        case 'SceneListChanged':
            dotNetHelper?.invokeMethodAsync('OnScenesChanged', JSON.stringify(filterAndSortScenes(data.eventData.scenes || [])));
            break;
        case 'CurrentProfileChanged':
            dotNetHelper?.invokeMethodAsync('OnProfileChanged', data.eventData.profileName);
            break;
        case 'CurrentSceneCollectionChanged':
            dotNetHelper?.invokeMethodAsync('OnSceneCollectionChanged', data.eventData.sceneCollectionName);
            break;
    }
}

// --- Public API ---

export function connect(host, port, pwd, helper) {
    dotNetHelper = helper;
    _password = pwd || '';
    _connectionError = false;
    pendingRequests.clear();

    if (ws) {
        ws.onclose = null; // suppress disconnect callback during reconnect
        ws.close();
        ws = null;
    }

    const url = `ws://${host}:${port}`;
    ws = new WebSocket(url);

    ws.onmessage = async (event) => {
        try {
            const msg = JSON.parse(event.data);
            await handleMessage(msg);
        } catch (err) {
            console.error('[OBS] Message handling error:', err);
        }
    };

    ws.onerror = () => {
        _connectionError = true;
        dotNetHelper?.invokeMethodAsync('OnConnectionError', `Unable to connect to OBS at ${host}:${port}. Ensure OBS is running and the WebSocket server is enabled.`);
    };

    ws.onclose = () => {
        // Only notify disconnected on a clean close, not after an error (error already notified)
        if (!_connectionError) {
            dotNetHelper?.invokeMethodAsync('OnDisconnected');
        }
        _connectionError = false;
    };
}

export function disconnect() {
    if (ws) {
        ws.onclose = null;
        ws.close();
        ws = null;
        dotNetHelper?.invokeMethodAsync('OnDisconnected');
    }
}

export async function switchScene(sceneName) {
    await sendRequest('SetCurrentProgramScene', { sceneName });
}

export async function switchPreviewScene(sceneName) {
    await sendRequest('SetCurrentPreviewScene', { sceneName });
}

export async function startStream() {
    await sendRequest('StartStream');
}

export async function stopStream() {
    await sendRequest('StopStream');
}

export async function startRecord() {
    await sendRequest('StartRecord');
}

export async function stopRecord() {
    await sendRequest('StopRecord');
}

export async function startVirtualCam() {
    await sendRequest('StartVirtualCam');
}

export async function stopVirtualCam() {
    await sendRequest('StopVirtualCam');
}

export async function startReplayBuffer() {
    await sendRequest('StartReplayBuffer');
}

export async function stopReplayBuffer() {
    await sendRequest('StopReplayBuffer');
}

export async function saveReplayBuffer() {
    await sendRequest('SaveReplayBuffer');
}

export async function setStudioMode(enabled) {
    await sendRequest('SetStudioModeEnabled', { studioModeEnabled: enabled });
}

export async function transitionToProgram() {
    await sendRequest('TriggerStudioModeTransition');
}

export async function setProfile(profileName) {
    await sendRequest('SetCurrentProfile', { profileName });
}

export async function setSceneCollection(sceneCollectionName) {
    await sendRequest('SetCurrentSceneCollection', { sceneCollectionName });
}
