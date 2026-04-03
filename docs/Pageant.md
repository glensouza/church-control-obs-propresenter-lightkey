# Pageant Scripted Production — Usage Guide

The Pageant features in Worship Console are designed for theatrical or highly scripted productions (like Christmas or Easter pageants) where camera cues, lighting changes, and OBS scene switches are predefined in a script.

---

## 1. Overview

Pageant is split into three main views, each tailored for a specific role:

| Page | Purpose | URL |
|---|---|---|
| **Script Viewer** | Master view of the entire script, acts, and scenes. | `/pageant` |
| **Livestream Cues** | Focused view for the OBS/Camera operator with automated execution. | `/pageant/livestream` |
| **Lighting Cues** | Focused view for the lighting/spotlight team. | `/pageant/lights` |

---

## 2. Script Data

The script data is stored in the local SQLite database.

Each script entry (scene) contains:
- **Act & Scene Number:** For organization and filtering.
- **Script Text:** The actual dialogue or action for that scene.
- **Length:** Estimated duration in seconds.
- **OBS Scene:** The authorized OBS scene that should be active.
- **Camera Cues:** Specific preset triggers (1-99) for up to 4 cameras.
- **Lighting Cues:** Stage scenes, house light settings, and spotlight assignments.

---

## 3. Livestream Cues (`/pageant/livestream`)

This page is designed for high-stakes scripted execution.

- **Sticky Controls:** The **Previous**, **Next**, and **TAKE** buttons are pinned to the top of the screen so they are always accessible during scrolling.
- **Auto-Scroll Navigation:** The left sidebar highlights the active scene. When navigating, the active scene automatically scrolls to the top of the list.
- **Sequential Execution (TAKE):**
    1. Clicking **TAKE** first triggers the **OBS Scene Switch**.
    2. Once the scene switch command is sent, all **Camera Presets** are triggered simultaneously.
- **Countdown Timer:** Clicking **TAKE** starts a real-time countdown from the defined `SceneLength`. The timer pulses in teal to provide a clear visual cue of remaining time.
- **Friendly Names:** Displays "Friendly Names" (e.g., "Narrator Background") mapped in the Admin Config for OBS scenes, while showing the technical scene name in parentheses.
- **Camera Cues:** Shows the specific VISCA preset number (1-99) for each configured camera.

---

## 4. Master Script (`/pageant`) — Script Editor

The master view allows for full editing of the production cues.

- **OBS Scene Selection:** A dropdown populated from "Authorized Mappings" (defined in Admin) and all current OBS scenes.
- **Camera Preset Logic:** 
    - Each camera cue has a **None / Set** toggle.
    - Choosing **Set** enables a numeric input for VISCA presets **1 through 99**.
    - Choosing **None** clears the preset and disables the input.
- **Filtering:** Use the **Act** dropdown to focus on a specific segment of the show.
- **Automatic Renumbering:** The system automatically handles scene numbering and ordering within acts when inserting or deleting scenes.

---

## 5. Pageant Configuration (Admin)

Authorized scenes and mappings are managed in the **Administration → Pageant Config** tab.

- **Authorized OBS Scenes:** Create friendly aliases for complex OBS scene names (e.g., Mapping "Main Stage" to `SCENE_LIVE_01_FINAL`). This simplifies the script editor and improves readability for operators.
- **Scene Mapping:** When editing, you can select from a live list of scenes fetched directly from OBS.
