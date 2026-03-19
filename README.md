# Worship Console

A unified AV control panel for worship teams, combining OBS, ProPresenter, and Lightkey into a single browser-based interface.

---

## Documentation

- **[Administration](docs/Administration.md)** — Manage hardware settings and media paths.
- **[OBS Control](docs/OBS.md)** — Scene switching and output management.
- **[Camera Control](docs/Cameras.md)** — PTZ and PoE power management.
- **[ProPresenter](docs/ProPresenter.md)** — Slide and media management.
- **[Planning Center](docs/PlanningCenter.md)** — Team and schedule integration.
- **[Pageant Production](docs/Pageant.md)** — Scripted production cues and sync.
- **[Lighting Control](docs/Lights.md)** — Cues and planned Lightkey integration.

---

## Features

- **OBS Control** — Full remote control via WebSocket v5. Switch scenes, manage streaming/recording, and toggle Studio Mode.
- **Camera Control** — Remote PTZ (Pan, Tilt, Zoom) for VISCA-over-IP cameras and UniFi PoE power management for hard reboots.
- **ProPresenter** — Remote slide advancement with thumbnails, media uploads (Welcome/Backgrounds), and a built-in YouTube downloader.
- **Pageant** — Centralized scripted production dashboard with dedicated views for directors, livestream operators, and lighting teams.
- **Planning Center** — Real-time visibility of scheduled team members for upcoming services.
- **Lightkey** — Lighting cue triggers and intensity control *(coming soon)*.

---

## OBS Page

The OBS control page connects directly to OBS Studio via the built-in WebSocket server (OBS-WebSocket v5). The connection target is configured server-side in `appsettings.json` — no URL entry is required by the operator.

### Attribution

The OBS control features are inspired by **[obs-web](https://github.com/Niek/obs-web)** by [Niek van der Maas](https://github.com/Niek) and contributors, released under the [GPL-3.0 License](https://github.com/Niek/obs-web/blob/master/LICENSE). obs-web is a browser-based remote control for OBS Studio that served as the reference for the feature set implemented in this project.

---

## Requirements

- [OBS Studio](https://obsproject.com/) v28 or higher (includes OBS-WebSocket v5)
- OBS WebSocket server enabled: `Tools → obs-websocket Settings → Enable WebSocket Server`
- .NET 10 runtime

## Getting Started

1. Clone the repository
2. Open the **Administration** page (`/admin`) in Worship Console and enter the OBS machine's IP address.
3. Ensure the `OBS:Password` is set in `WorshipConsole/appsettings.json`.
3. Run the application: `dotnet run --project WorshipConsole`
4. Open the browser and navigate to `/obs`
