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

## 3. Navigating to the ProPresenter Pages

In the Worship Console web app, click **Media → ProPresenter** to access the main hub, or use the sub-navigation to go directly to:
- **[Slides](#4-slide-control-slides)** (`/slides`)
- **[Media Management](#5-media-management-media)** (`/media`)

---

## 4. Slide Control (`/slides`)

The Slides page provides a real-time remote for your active ProPresenter presentation.

### Features
- **Live Preview:** View the current slide being displayed in ProPresenter.
- **Navigation:** Large, touch-friendly **Next** and **Previous** buttons.
- **Thumbnail Preview:** See the upcoming and previous slides as small thumbnails before you switch to them.
- **Clearing:** Individual buttons to **Clear Slide** (keep background/props) or **Clear All Layers** (emergency blackout).

---

## 5. Media Management (`/media`)

The Media page allows you to manage local video files on the ProPresenter machine and download content from YouTube.

### Welcome Video
Upload and manage the "Welcome" loop video that plays before service. The system automatically handles renaming and placement in the configured `Welcome` folder.

### Background Playlists
Upload multiple motion backgrounds or video loops.
- **Preview:** Click the eye icon to watch any video directly in the browser.
- **Thumbnail Generation:** The console automatically generates JPEG thumbnails for uploaded videos using `FFmpeg`.

### YouTube Downloader
Paste any YouTube URL to download the video or just the audio (MP3) directly to the ProPresenter server.
- **Formats:** Choose between high-quality video or audio-only.
- **Automatic Storage:** Files are saved to the `YouTubeDownloads` folder configured in `appsettings.json`.
- **Thumbnails:** The downloader also saves the video's thumbnail for easy identification.

---

## 6. Configuration (Advanced)

The media features require `FFmpeg` to be installed on the server for thumbnail generation.

```json
{
  "ProPresenter": {
    "Host": "10.10.10.245",
    "Port": 20000,
    "MediaRootPath": "C:\\Media",
    "FfmpegPath": "C:\\FFmpeg\\bin",
    "WelcomeVideoFolder": "Welcome",
    "BackgroundVideosFolder": "Backgrounds",
    "YouTubeDownloadsFolder": "YouTube"
  }
}
```

---

## 7. Troubleshooting

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
