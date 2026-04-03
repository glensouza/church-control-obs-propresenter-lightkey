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

---

## 2. Configure Administration

Open the **Administration** page (`/admin`) in Worship Console and fill in your OBS machine's details:

1.  Navigate to the **OBS Settings** tab.
2.  Enter the **Host** (IP address) and **WebSocket Port** (default: `4455`).
3.  Click **Save OBS Settings**.

> **Note:** For security, the **OBS Password** must still be set in `WorshipConsole/appsettings.json`.

### Pageant OBS Configuration
To use OBS scenes effectively within the **Pageant** scripted system, you can define friendly aliases:

1.  Navigate to the **Pageant Config** tab in Administration.
2.  Add a **Scene Mapping**.
3.  Assign a **Friendly Name** (e.g., "Background A") to an actual **OBS Scene Name**.
4.  These friendly names will appear in the script editor and the operator cues.

---

## 3. Navigating to the OBS Page

In the Worship Console web app, click **Livestream → OBS** in the top navigation bar. The page will automatically attempt to connect to OBS when it loads.

---

## 4. Connection Status

The top-right of the OBS page (and the Pageant Livestream page) shows the current connection status:

| Badge | Meaning |
|---|---|
| 🟢 **Connected** | Successfully connected and ready |
| 🟡 **Connecting…** | Attempting to establish connection |
| 🔴 **Error** | Connection failed — see error message below the badge |
| ⚫ **Disconnected** | Manually disconnected |

---

## 5. Switching Scenes

### Manual Scene Switching
On the main OBS page, click any scene button to switch immediately.

### Automated Scene Switching (Pageant)
In the **Pageant Livestream** view, scene switches are triggered by the **TAKE** button.
- **Sequential Priority:** The OBS scene switch is executed first, followed by camera movements.
- **Visual Mapping:** If a mapping exists in Pageant Config, the operator will see the friendly alias instead of the technical scene name.

---

## 6. Studio Mode

Toggle **Studio Mode** using the switch in the toolbar. In this mode, you can prepare a scene in **Preview** before pushing it to **Program**.

---

## 7. Output Controls

Large buttons at the bottom control:
- **Start / Stop Stream**
- **Start / Stop Recording**
- **Start / Stop Virtual Camera**
- **Replay Buffer** management

---

## Troubleshooting

### "Unable to connect to OBS"
- Verify OBS is open and the WebSocket server is enabled.
- Check firewall settings for port 4455.
- Ensure the IP address in Admin settings is correct.

### "Friendly Name not appearing"
- Ensure the mapping is correctly defined in the **Administration → Pageant Config** tab.
- Refresh the Pageant Livestream page to reload mappings.
