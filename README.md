# Worship Console

A unified AV control panel for worship teams, combining OBS, ProPresenter, and Lightkey into a single browser-based interface.

---

## Features

- **OBS Control** — Switch scenes, manage streaming and recording, studio mode, virtual camera, replay buffer, profile/scene-collection switching
- **ProPresenter** — Slide advancement and media control *(coming soon)*
- **Lightkey** — Lighting cue triggers and intensity control *(coming soon)*

---

## OBS Page

The OBS control page connects directly to OBS Studio via the built-in WebSocket server (OBS-WebSocket v5). The connection target is configured server-side in `appsettings.json` — no URL entry is required by the operator.

See **[docs/OBS.md](docs/OBS.md)** for full setup and usage instructions.

### Attribution

The OBS control features are inspired by **[obs-web](https://github.com/Niek/obs-web)** by [Niek van der Maas](https://github.com/Niek) and contributors, released under the [GPL-3.0 License](https://github.com/Niek/obs-web/blob/master/LICENSE). obs-web is a browser-based remote control for OBS Studio that served as the reference for the feature set implemented in this project.

---

## Requirements

- [OBS Studio](https://obsproject.com/) v28 or higher (includes OBS-WebSocket v5)
- OBS WebSocket server enabled: `Tools → obs-websocket Settings → Enable WebSocket Server`
- .NET 10 runtime

## Getting Started

1. Clone the repository
2. Set the OBS host/port/password in `WorshipConsole/appsettings.json`
3. Run the application: `dotnet run --project WorshipConsole`
4. Open the browser and navigate to `/obs`
