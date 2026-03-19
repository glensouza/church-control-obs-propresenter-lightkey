# Lighting Control — Usage Guide

The Lighting features in Worship Console provide visibility and control over stage lighting cues and intensity.

---

## 1. Pageant Lighting (`/pageant/lights`)

The Pageant Lighting view is a specialized dashboard for scripted productions. It displays pre-defined cues for the lighting team, including:

- **Stage Scene:** The active look to be recalled on the lighting console.
- **Spotlight Assignments:** High-visibility targets for left and right spotlight operators.
- **House Light Status:** Guidance for the auditorium lighting (e.g., "Fade to 20%").
- **Lighting Notes:** Scene-specific instructions or timing cues.

For more details on scripted productions, see **[Pageant Production](Pageant.md)**.

---

## 2. Integrated Lighting Control (`/lights`)

The dedicated **Lights** page is currently a placeholder for direct integration with lighting control software.

### Planned Features:
- **Lightkey Integration:** Remote trigger for Lightkey live cues and presets.
- **DMX-over-IP:** Basic intensity and color control via Art-Net or sACN.
- **Preset Buttons:** Quick-access buttons for common looks (e.g., "Full Wash", "Sermon", "Blackout").

---

## 3. Configuration

Currently, lighting cues are managed via the **Pageant Script** in the database. When the Lightkey integration is released, configuration will be managed in `appsettings.json` and through a dedicated admin UI.
