# Camera Control — Setup & Usage Guide

The Cameras page in Worship Console provides remote PTZ (Pan, Tilt, Zoom) control for VISCA-over-IP compatible cameras. It also integrates with UniFi network switches to provide remote power-over-ethernet (PoE) control, allowing you to hard-reboot cameras directly from the interface.

---

## 1. Camera Requirements

| Requirement | Details |
|---|---|
| Protocol | VISCA over IP (TCP) |
| Port | Usually `5678` or `1259` (configurable) |
| PoE Control | Requires a compatible UniFi Switch and `UniFiService` configuration |

---

## 2. Configuration (Admin Only)

To manage the list of cameras, navigate to **Livestream → Administration** and select the **Camera Admin** tab.

### Adding a Camera
1. Click **Add Camera**.
2. Enter a **Name** (e.g., "Center Cam").
3. Enter the **IP Address** of the camera.
4. Enter the **VISCA Port** (default for many PTZ cameras is `5678`).
5. Enter the **UniFi Port Number** (optional) if the camera is powered by a UniFi PoE switch.
6. Click the **Checkmark (Save)** button.

---

## 3. Controlling Cameras

### Manual Dashboard (`/livestream/cameras`)
Used for spontaneous, non-scripted camera movements.
- **PTZ Controls:** Directional arrows, Zoom +/-.
- **Home:** Returns camera to default.
- **Presets 1-9:** Grid of buttons for quick recall of the primary 9 presets.

### Pageant Automated Cues (`/pageant/livestream`)
Used for scripted productions.
- **Triggering:** Camera presets are automatically recalled when the **TAKE** button is clicked.
- **Preset Range:** Supports VISCA presets **1 through 99**.
- **Configuration:** Presets are assigned to scenes via the **Pageant Landing Page** (Script Editor).
- **Behavior:** Cameras trigger simultaneously immediately after the OBS scene transition begins.

---

## 4. Presets

### Manual Control (Presets 1-9)
The grid of numbered buttons on the manual dashboard corresponds to camera presets 1 through 9.
- **Recall:** Click a number to immediately move the camera to that saved preset.

### Pageant Control (Presets 1-99)
The Pageant system allows you to trigger any preset stored on the camera (1-99).
- **Set/None:** In the script editor, you can toggle a camera cue to **Set** to provide a specific preset number, or **None** to ignore that camera for the scene.

---

## 5. Troubleshooting

### "No IP address configured..."
- The camera entry exists in the database but the IP address field is empty. Go to the Admin page and provide a valid IP.

### Controls are unresponsive
- Verify the camera is powered on.
- Verify the **VISCA Port** in Admin matches the camera's settings.
- Ensure the Worship Console server can "ping" the camera's IP address.
- Some cameras require "VISCA over UDP" instead of TCP; currently, only TCP is supported.
