# Worship Console - Project Context

A unified AV control panel for worship teams, combining OBS, ProPresenter, and Lightkey into a single browser-based interface.

## Project Overview

- **Type:** .NET 10 Blazor Web Application (Interactive Server Mode).
- **Core Purpose:** Provide a simplified, centralized web interface for controlling AV equipment during worship services or events.
- **Key Integrations:**
    - **OBS Studio:** Full remote control via OBS-WebSocket v5 (switching scenes, managing streams/recordings, studio mode).
    - **Cameras:** PTZ camera control via Visca protocol.
    - **Networking:** Port management on UniFi switches (e.g., for power cycling cameras).
    - **Planning Center Online (PCO):** API integration for service planning and volunteer roles.
    - **ProPresenter & Lightkey:** Planned integrations for media and lighting control.
- **Architecture:**
    - **Frontend:** Blazor components (`Components/Pages`) with Vanilla CSS.
    - **Backend:** ASP.NET Core with Scoped and Singleton services for hardware communication.
    - **Database:** SQLite managed by Entity Framework Core for storing "Scripts" (production cues) and "Settings".

## Building and Running

### Prerequisites
- .NET 10 SDK
- OBS Studio v28+ (with WebSocket server enabled)

### Key Commands
- **Build:** `dotnet build`
- **Run:** `dotnet run --project WorshipConsole`
- **Database Migrations:**
    - Add a migration: `dotnet ef migrations add <MigrationName> --project WorshipConsole`
    - Update database: `dotnet ef database update --project WorshipConsole`
    - *Note:* The application automatically applies pending migrations on startup in `Program.cs`.

### Configuration
Configuration is managed in `WorshipConsole/appsettings.json`. Key sections include:
- `ConnectionStrings:PageantDb`: SQLite connection string.
- `OBS`: Host, Port, and Password for OBS WebSocket.
- `UniFi`: Credentials and target switch details.
- `Cameras`: List of PTZ cameras with IP and Visca details.
- `Pco`: Planning Center Online API credentials.

## Development Conventions

- **Language:** C# 13+ with file-scoped namespaces and implicit usings.
- **Components:**
    - Located in `WorshipConsole/Components/Pages`.
    - Use `InteractiveServer` render mode.
    - **Styling:** CSS isolation is **mandatory**. All component-specific styles must reside in a corresponding `.razor.css` file.
    - **No Inline Styles:** Never use the `style="..."` attribute in `.razor` files unless the value is truly dynamic and calculated at runtime.
    - **No Style Blocks:** Never use `<style>` tags within `.razor` files.
- **Services:**
    - `ObsWebSocketService`: Scoped (one instance per Blazor circuit) to maintain connection state.
    - `UniFiService` / `ViscaService`: Singletons for shared hardware access.
    - `PcoApiService`: Scoped with `HttpClient` factory.
- **Database:**
    - Models are in `WorshipConsole/Models`.
    - Context is `WorshipConsole.Database.PageantDb`.
    - Uses `IDbContextFactory` for thread-safe access in Blazor.
- **OBS Integration:**
    - Uses server-side WebSocket client to hide credentials from the browser.
    - Inspired by `obs-web` features but reimplemented in C#.
- **Pageant Logic:**
    - The "Pageant" features (`Pageant.razor`, `PageantDb.cs`) are designed for theatrical/scripted productions where cameras, lighting, and OBS scenes are triggered based on a script.

## Directory Structure
- `WorshipConsole/Components/Pages`: Blazor UI pages.
- `WorshipConsole/Services`: Core logic for external integrations.
- `WorshipConsole/Models`: POCO classes for DB and API data.
- `WorshipConsole/Database`: EF Core context and migrations.
- `docs/`: Additional documentation (e.g., `OBS.md`, `PlanningCenter.md`).
