# Administration

The Administration module provides a centralized interface for configuring the Worship Console and managing hardware integrations.

## Accessing Administration

The Admin panel can be accessed via:
1.  **Navigation Bar**: `Livestream → Administration`
2.  **Direct URL**: `/admin`

## Configuration Tabs

### 1. General Settings
This tab manages core integration settings for general church information and social media (YouTube/Facebook handles).

### 2. OBS Settings
This tab manages core integration settings for OBS Studio.

*   **Host**: The IP address of the computer running OBS (default: `127.0.0.1`).
*   **WebSocket Port**: The port configured in OBS WebSocket settings (default: `4455`).
*   *Note: The OBS Password must still be set in `appsettings.json` for security.*

### 3. Camera Admin
Manage the list of PTZ cameras and their network/control settings.

*   **Add/Edit/Delete**: Manage the list of PTZ cameras.
*   **VISCA Port**: Usually `5678`.
*   **UniFi Port**: The port number on the UniFi switch used for PoE power cycles (optional).
*   **Presets**: Number of presets supported by the camera (usually `9`).

### 4. ProPresenter
Configure the connection to ProPresenter and media folder locations.

*   **Host/Port**: Connection details for the ProPresenter Network API.
*   **Media Root Path**: The base directory on the Worship Console server where media files are stored.
*   **FFmpeg Path**: Path to the FFmpeg binary or folder for video processing/thumbnails.
*   **Folder Names**: Configure the specific subfolders for Welcome videos, Backgrounds, and YouTube downloads.

### 5. Planning Center
Integration settings for Planning Center Online (PCO).

*   **Position Names**: Customize the team position names used to identify volunteers for ProPresenter, Livestream, and Worship Coordinator roles.

## Technical Details

Settings are stored in the SQLite database (`Settings` table) via the `SettingsService`. 

### Initialization & Seeding
Upon first run, the system automatically seeds the database with values from `appsettings.json` (if they exist) or project-standard defaults. This is handled in `Program.cs` via `SettingsService.InitializeFromConfigAsync()`.

Subsequent changes in the Admin UI update the database directly and take precedence over `appsettings.json`. 

### Secrets
Sensitive information (Passwords, API Secrets) is **never** stored in the database or displayed in the UI. These must be managed via `appsettings.json`.
