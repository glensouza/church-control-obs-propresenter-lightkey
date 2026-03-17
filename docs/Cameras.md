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

To manage the list of cameras, navigate to **Livestream → Cameras** and click the **Admin** button (or go directly to `/admin/cameras`).

### Adding a Camera
1. Click **Add Camera**.
2. Enter a **Name** (e.g., "Center Cam").
3. Enter the **IP Address** of the camera.
4. Enter the **VISCA Port** (default for many PTZ cameras is `5678`).
5. Enter the **UniFi Port Number** (optional) if the camera is powered by a UniFi PoE switch.
6. Click the **Checkmark (Save)** button.

### Editing/Deleting
- Use the **Pencil** icon to edit an existing camera's details.
- Use the **Trash** icon to remove a camera from the system.

---

## 3. Controlling Cameras

Navigate to **Livestream → Cameras** to access the control dashboard.

### Power Control (PoE)
If a UniFi Port is configured, you will see a power toggle in the camera's header.
- **Toggle Switch:** Turns PoE power on or off for that specific switch port.
- **Status Badge:** Shows the real-time power state returned by the UniFi controller.

> ⚠️ **Warning:** Turning off power will disconnect the camera. It usually takes 30-60 seconds for a camera to reboot and rejoin the network after power is restored.

### PTZ Controls (Pan, Tilt, Zoom)
- **Directional Buttons:** Click and hold to move the camera. Release to stop.
- **Home (House icon):** Returns the camera to its default "home" position.
- **Zoom (+/-):** Click and hold to zoom in or out.
- **Focus:** 
    - **In/Out:** Manual focus adjustment (hold to move).
    - **Auto (Camera icon):** Triggers the camera's internal auto-focus routine.

### Presets
The grid of numbered buttons corresponds to camera presets 1 through 9.
- **Layout:** The buttons are arranged in a 3x3 grid matching a standard keyboard numpad for intuitive spatial mapping.
- **Recall:** Click a number to immediately move the camera to that saved preset.

---

## 4. Troubleshooting

### "No IP address configured..."
- The camera entry exists in the database but the IP address field is empty. Go to the Admin page and provide a valid IP.

### Controls are unresponsive
- Verify the camera is powered on.
- Verify the **VISCA Port** in Admin matches the camera's settings.
- Ensure the Worship Console server can "ping" the camera's IP address.
- Some cameras require "VISCA over UDP" instead of TCP; currently, only TCP is supported.

### PoE Toggle is disabled
- Ensure UniFi settings are correctly configured in `appsettings.json`.
- Verify the **UniFi Port Number** in Camera Admin matches the actual physical port on your switch.
