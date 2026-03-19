# Pageant Scripted Production — Usage Guide

The Pageant features in Worship Console are designed for theatrical or highly scripted productions (like Christmas or Easter pageants) where camera cues, lighting changes, and OBS scene switches are predefined in a script.

---

## 1. Overview

Pageant is split into three main views, each tailored for a specific role:

| Page | Purpose | URL |
|---|---|---|
| **Script Viewer** | Master view of the entire script, acts, and scenes. | `/pageant` |
| **Livestream Cues** | Focused view for the OBS/Camera operator. | `/pageant/livestream` |
| **Lighting Cues** | Focused view for the lighting/spotlight team. | `/pageant/lights` |

---

## 2. Script Data

The script data is stored in the local SQLite database (`pageant.db`). Currently, entries must be added directly to the database or via a future admin interface.

Each script entry (scene) contains:
- **Act & Scene Number:** For organization and filtering.
- **Script Text:** The actual dialogue or action for that scene.
- **Length:** Estimated duration in seconds.
- **OBS Scene:** The name of the OBS scene that should be active.
- **Camera Cues:** Specific instructions for up to 3 cameras (e.g., "Wide", "Tight on Narrator").
- **Lighting Cues:** Stage scenes, house light settings, and spotlight assignments.

---

## 3. Livestream Cues (`/pageant/livestream`)

This page is designed to sit alongside the OBS control panel.

- **Navigation:** Use the left sidebar to jump to any scene, or use the **Next/Previous** buttons to step through the script.
- **OBS Sync:** If an OBS scene is specified for the current script entry, it is highlighted in a large blue banner.
- **Camera Cues:** A 3-column layout shows the specific target for each of the three PTZ cameras.

---

## 4. Lighting Cues (`/pageant/lights`)

This page provides the lighting team with a clear, distraction-free view of their cues.

- **Scene Display:** Shows the "Stage Light Scene" name (matching your lighting console's labels).
- **Spotlights:** Large, high-visibility boxes show targets for **Spotlight Left** and **Spotlight Right**.
- **House Lights:** Indicates the status of auditorium lighting (e.g., "Down", "Half", "Up").
- **Notes:** Displays any specific timing or transition notes for the lighting team.

---

## 5. Master Script (`/pageant`)

The master view allows the director or stage manager to see the "big picture."

- **Filtering:** Use the **Act** dropdown to focus on a specific segment of the show.
- **Detail View:** Clicking any scene card opens a full detail panel showing every cue (OBS, Camera, and Lights) simultaneously.
- **Timing:** Shows the total number of scenes and the duration of each.
