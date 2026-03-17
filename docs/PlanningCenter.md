# Planning Center Online (PCO) — Setup & Configuration

Worship Console integrates with Planning Center Services to automatically fetch scheduled team members (e.g., ProPresenter operator, Livestream operator) for upcoming services.

---

## Requirements

| Requirement | Details |
|---|---|
| PCO Account | Access to Planning Center Services with at least "Viewer" permissions |
| Personal Access Token | Generated from the Planning Center API portal |

---

## 1. Generate API Credentials

Worship Console uses **Personal Access Tokens (PATs)** for authentication.

1.  Log in to [Planning Center API Developer Portal](https://api.planningcenteronline.com/).
2.  Navigate to **Personal Access Tokens**.
3.  Click **Create a New Personal Access Token**.
4.  Give it a name (e.g., "Worship Console").
5.  After clicking Create, you will be shown:
    - **Application ID** (This is your `AppId`)
    - **Secret** (This is your `AppSecret`)
    
> ⚠️ **Warning:** The Secret is only shown once. Copy it immediately to a safe place.

---

## 2. Find your Service Type ID

Each group of services in Planning Center (e.g., "Saturday Service", "Youth Group") has a unique `ServiceTypeId`.

1.  Open **Planning Center Services** in your browser.
2.  Click on the **Service Type** you want to track.
3.  Look at the URL in your browser's address bar. It will look like this:
    `https://services.planningcenteronline.com/service_types/1234567`
4.  The number at the end (`1234567`) is your **Service Type ID**.

---

## 3. Configure appsettings.json

Open `WorshipConsole/appsettings.json` and fill in the `Pco` section:

```json
{
  "Pco": {
    "AppId": "your_application_id",
    "AppSecret": "your_secret",
    "ServiceTypeId": "1234567",
    "ProPresenterPosition": "ProPresenter",
    "LivestreamPosition": "Livestream",
    "WorshipCoordinatorPosition": "Worship Coordinator"
  }
}
```

### Configuration Fields

| Field | Description |
|---|---|
| `AppId` | The "Application ID" from PCO PAT. |
| `AppSecret` | The "Secret" from PCO PAT. |
| `ServiceTypeId` | The numeric ID of the service type to fetch plans from. |
| `ProPresenterPosition` | The exact name (or partial name) of the team position for ProPresenter. |
| `LivestreamPosition` | The exact name (or partial name) of the team position for Livestream. |
| `WorshipCoordinatorPosition` | The exact name (or partial name) of the team position for the Worship Coordinator. |

---

## 4. Usage in Worship Console

The PCO integration is primarily used on the **Contact** page (`/contact`) and potentially other summary pages to show who is serving.

The system specifically looks for **future plans** and filters for the **next Saturday** (as configured in `PcoApiService.cs`). If your services are on Sundays, you may need to adjust the logic in `PcoApiService.cs`.

---

## Troubleshooting

### "PCO:AppId is not configured"
- Ensure `appsettings.json` has valid values for both `AppId` and `AppSecret`.
- Ensure the file is being copied to the output directory (Build Action: Content, Copy if newer).

### No team members showing up
- Verify the `ServiceTypeId` is correct.
- Ensure the team members are **Confirmed** in Planning Center.
- Check that the `ProPresenterPosition` and other position names match exactly what is in PCO (case-insensitive partial match is supported).
