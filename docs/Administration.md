# Administration

The Administration module provides a centralized interface for configuring the Worship Console and managing hardware integrations.

## Accessing Administration

The Admin panel can be accessed via:
1.  **Navigation Bar**: `Livestream → Administration`
2.  **Direct URL**: `/admin`

*Note: The Admin page defaults to the **General Settings** tab.*

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
*   **Presets**: Number of presets supported by the camera (usually `9` for manual control, though Pageant mode supports up to `99`).

### 4. ProPresenter
Configure the connection to ProPresenter and media folder locations.

*   **Host/Port**: Connection details for the ProPresenter Network API.
*   **Media Root Path**: The base directory on the Worship Console server where media files are stored.
*   **FFmpeg Path**: Path to the FFmpeg binary or folder for video processing/thumbnails.
*   **Folder Names**: Configure the specific subfolders for Welcome videos, Backgrounds, and YouTube downloads.

### 5. Planning Center
Integration settings for Planning Center Online (PCO).

*   **Position Names**: Customize the team position names used to identify volunteers for ProPresenter, Livestream, and Worship Coordinator roles.

### 6. Livestream
Persistent stream IDs and default scheduling times for YouTube and Facebook APIs.

### 7. Pageant Config
A specialized tab for managing scripted production requirements.

*   **Authorized Pageant OBS Scenes**: Map friendly names (labels) to actual OBS scene names. These friendly names are used in the Pageant Livestream view to make cues more readable for operators.
*   **Scene Dropdown**: When adding or editing a mapping, you can select from a live list of scenes fetched from OBS.

## Technical Details

Settings are stored in the SQLite database (`Settings` table) via the `SettingsService`. 

### Secrets
Sensitive information (Passwords, API Secrets) is **never** stored in the database or displayed in the UI. These must be managed via `appsettings.json`.
