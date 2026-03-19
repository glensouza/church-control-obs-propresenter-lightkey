# OBS Control — Setup & Usage Guide

The OBS page in Worship Console gives you full remote control of OBS Studio from any browser on your network. It connects to OBS's built-in WebSocket server using a static IP address configured by your system administrator.

---

## Requirements

| Requirement | Details |
|---|---|
| OBS Studio | v28 or higher (ships with OBS-WebSocket v5) |
| Network | Worship Console server and OBS PC must be on the same local network |
| OBS WebSocket | Must be enabled in OBS settings (see below) |

---

## 1. Enable OBS WebSocket Server

1. Open **OBS Studio**.
2. Go to **Tools → obs-websocket Settings**.
3. Check **Enable WebSocket Server**.
4. Note the **Server Port** (default: `4455`).
5. Optionally set a **Server Password** for security.
6. Click **OK**.

> **Tip:** Write down the IP address of the OBS machine — you can find it in your OS network settings (e.g., `192.168.1.50`).

---

## 2. Configure Administration

Open the **Administration** page (`/admin`) in Worship Console and fill in your OBS machine's details:

1.  Navigate to the **General Settings** tab.
2.  Enter the **Host** (IP address) and **WebSocket Port** (default: `4455`).
3.  Click **Save All General Settings**.

> **Note:** For security, the **OBS Password** must still be set in `WorshipConsole/appsettings.json`.

```json
"OBS": {
  "Password": "your-obs-websocket-password"
}
```

}
```

| Field | Description | Default |
|---|---|---|
| `Host` | Static IP address of the OBS machine | `127.0.0.1` |
| `Port` | OBS WebSocket server port | `4455` |
| `Password` | OBS WebSocket server password (leave empty if none) | *(empty)* |

After saving, restart the Worship Console application.

---

## 3. Navigating to the OBS Page

In the Worship Console web app, click **Livestream → OBS** in the top navigation bar. The page will automatically attempt to connect to OBS when it loads.

---

## 4. Connection Status

The top-right of the OBS page shows the current connection status:

| Badge | Meaning |
|---|---|
| 🟢 **Connected** | Successfully connected and ready |
| 🟡 **Connecting…** | Attempting to establish connection |
| 🔴 **Error** | Connection failed — see error message below the badge |
| ⚫ **Disconnected** | Manually disconnected |

If an error occurs, an error banner will appear with a description. Common causes:
- OBS is not running
- The IP address or port in `appsettings.json` is incorrect
- The OBS WebSocket server is not enabled
- The password does not match

Click **Reconnect** to try again without reloading the page.

---

## 5. Switching Scenes

When connected, all your OBS scenes appear as buttons in the main area.

- **Click a scene button** to immediately switch to it (Live switch).
- The **currently active scene** is highlighted in teal.
- Scenes named with `(hidden)` in their name are automatically hidden from this list — useful for internal scenes you never want to switch to by accident.

---

## 6. Studio Mode

Toggle **Studio Mode** using the switch in the toolbar.

In Studio Mode the scene list splits into two columns:

| Column | Description |
|---|---|
| **Preview** (teal) | The scene being prepared off-air |
| **Program** (red) | The scene currently on-air |

- Click a scene in the **Preview** column to load it into the preview monitor.
- Click a scene in the **Program** column to cut directly to it (bypasses preview).
- Click **⇒ Transition to Program** to push the preview scene to program using the current transition.

---

## 7. Output Controls

The row of large buttons at the bottom controls OBS outputs:

| Button | Action |
|---|---|
| **Start / Stop Stream** | Starts or stops the configured streaming output |
| **Start / Stop Recording** | Starts or stops local recording |
| **Start / Stop Virtual Camera** | Enables or disables the OBS Virtual Camera |
| **Start Replay Buffer** | Begins buffering recent footage |
| **Save Replay** | Saves the current buffer contents to disk |
| **Stop Buffer** | Stops the replay buffer |

All button states update in real time — if someone changes a setting directly in OBS, the page reflects it immediately.

---

## 8. Profile Switching

The **Profile** dropdown in the toolbar lets you switch between OBS output profiles (e.g., "Sunday Service", "Rehearsal"). The page refreshes the scene list automatically after switching.

---

## 9. Scene Collection Switching

The **Scene Collection** dropdown lets you switch between different sets of scenes. This is useful if you maintain separate scene collections for different events (e.g., "Sunday Morning", "Youth Group").

> ⚠️ Switching scene collections reloads all scenes in OBS. Make sure you are not live before switching.

---

## Troubleshooting

### "Unable to connect to OBS at …"
- Verify OBS is open and running.
- Verify the IP address and port in `appsettings.json` match the OBS machine.
- Check that the OBS WebSocket server is enabled (`Tools → obs-websocket Settings`).
- Ensure no firewall is blocking port 4455 on the OBS machine.

### "OBS WebSocket requires a password"
- A password is set in OBS but `appsettings.json` has an empty `Password` field.
- Add the correct password and restart Worship Console.

### Scenes not updating
- Navigate away from the OBS page and back — this reconnects the WebSocket.
- Check the OBS log for any WebSocket errors.

---

## Attribution

The OBS control feature set is inspired by **[obs-web](https://github.com/Niek/obs-web)** by [Niek van der Maas](https://github.com/Niek) and contributors (GPL-3.0). The WebSocket protocol implementation follows the [OBS-WebSocket v5 specification](https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md).
