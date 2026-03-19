# Administration

The Administration module provides a centralized interface for configuring the Worship Console and managing hardware integrations.

## Accessing Administration

The Admin panel can be accessed via:
1.  **Navigation Bar**: `Livestream → Administration`
2.  **Direct URL**: `/admin`

## Configuration Tabs

### 1. General Settings
This tab manages core integration settings for OBS, ProPresenter, and Planning Center.

#### OBS Studio
*   **Host**: The IP address of the computer running OBS (default: `127.0.0.1`).
*   **WebSocket Port**: The port configured in OBS WebSocket settings (default: `4455`).
*   *Note: The OBS Password must still be set in `appsettings.json` for security.*

#### ProPresenter
*   **Host/Port**: Connection details for the ProPresenter Network API.
*   **Media Root Path**: The base directory on the Worship Console server where media files are stored.
*   **FFmpeg Path**: Path to the FFmpeg binary or folder for video processing/thumbnails.
*   **Folder Names**: Configure the specific subfolders for Welcome videos, Backgrounds, and YouTube downloads.

#### Planning Center Online (PCO)
*   **Position Names**: Customize the team position names used to identify volunteers for ProPresenter, Livestream, and Worship Coordinator roles in the "Next Service" summary.

### 2. Camera Admin
Replaces the old standalone camera admin page.

*   **Add/Edit/Delete**: Manage the list of PTZ cameras.
*   **VISCA Port**: Usually `5678`.
*   **UniFi Port**: The port number on the UniFi switch used for PoE power cycles (optional).
*   **Presets**: Number of presets supported by the camera (usually `9`).

## Technical Details

Settings are stored in the SQLite database (`Settings` table) via the `SettingsService`. 

### Initialization
Upon first run, the system seeds the database with values from `appsettings.json`. Subsequent changes in the Admin UI update the database directly. 

### Secrets
Sensitive information (Passwords, API Secrets) is **never** stored in the database or displayed in the UI. These must be managed via `appsettings.json`.
