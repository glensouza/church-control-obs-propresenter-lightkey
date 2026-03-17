# ProPresenter Control — Setup & Usage Guide

The ProPresenter integration in Worship Console allows you to remotely control slide playback, media playlists, and stage displays from any browser on your network. It connects to ProPresenter 7 using its built-in Network API.

---

## Requirements

| Requirement | Details |
|---|---|
| ProPresenter | v7.x or higher |
| Network | Worship Console server and ProPresenter PC must be on the same local network |
| Network API | Must be enabled in ProPresenter settings (see below) |

---

## 1. Enable ProPresenter Network API

1. Open **ProPresenter 7**.
2. Go to **Settings (or Preferences) → Network**.
3. Check **Enable Network**.
4. Check **Enable ProPresenter Network API** (This is **REQUIRED** for this console).
   - *Note: This is different from the "ProPresenter Remote" setting, which is for the mobile app.*
   - *Do not use the TCP/IP or Stage App ports for this console; those are raw sockets, not the HTTP Network API.*
5. Note the **Port** (default: `20000`).
6. Optionally set a **Password** for the API (if your version supports it).
7. Click **OK**.

---

## 2. Configure appsettings.json

Open `WorshipConsole/appsettings.json` and fill in your ProPresenter machine's details:

```json
{
  "ProPresenter": {
    "Host": "192.168.1.55",
    "Port": 20000,
    "Password": "your-password"
  }
}
```

> **IMPORTANT:** If you are running the console on a different machine than ProPresenter, do **not** use `127.0.0.1`. Use the actual local IP address of the ProPresenter PC (e.g., `192.168.1.55`).

| Field | Description | Default |
|---|---|---|
| `Host` | Static IP address of the ProPresenter machine | `127.0.0.1` |
| `Port` | ProPresenter Network API port | `20000` |
| `Password` | API password (leave empty if none) | *(empty)* |

---

## 3. Navigating to the ProPresenter Page

In the Worship Console web app, click **Media → ProPresenter** in the top navigation bar.

*Note: Integration for slide and media control is currently in development.*

---

## 4. Troubleshooting

### Connection Failed
- Verify ProPresenter is open and the machine is awake.
- Ensure the **Network API** is enabled in ProPresenter's Network settings.
- Check that the IP address in `appsettings.json` is correct.
- If using a password, ensure it matches exactly what is in ProPresenter.
- Verify no firewall is blocking port 20000 on the ProPresenter machine.
- If you see errors like "invalid status line" or 400 responses with `{ "error": "400 Bad Request" }`, you're likely pointed at the TCP/IP/Remote port. Switch back to the Network API port (default `20000`).

---

## Technical Details

The integration uses the **[ProPresenter 7 API](https://openapi.propresenter.com/)** for communication. Commands are sent as standard HTTP/REST requests.
