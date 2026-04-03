# Livestream Scheduling Setup (2026 Edition)

This guide explains how to set up the one-click scheduling feature for YouTube and Facebook, including thumbnail uploads.

## 1. YouTube API Setup

To schedule YouTube broadcasts, you need to create a Google Cloud Project and obtain OAuth credentials.

### A. Create a Google Cloud Project
1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project named "Worship Console".
3. Search for **YouTube Data API v3** and enable it.

### B. Create OAuth Credentials
1. Go to **APIs & Services > Credentials**.
2. Click **Create Credentials > OAuth client ID**.
3. Choose **Web application**.
4. Add `https://developers.google.com/oauthplayground` as an **Authorized redirect URI**.
5. Copy your **Client ID** and **Client Secret** to `appsettings.json`.

### C. Get a Refresh Token (Google OAuth Playground)
1. Go to [Google OAuth Playground](https://developers.google.com/oauthplayground).
2. Click the **Gear Icon** (top right) and set:
    - **OAuth flow**: `Server-side`
    - **OAuth endpoints**: `Custom`
    - **Authorization endpoint**: `https://accounts.google.com/o/oauth2/v2/auth`
    - **Token endpoint**: `https://oauth2.googleapis.com/token`
    - **Access token location**: `Authorization header with Bearer prefix`
    - **Access type**: `Offline` (Important!)
    - **Force prompt**: `Consent`
    - **Use your own OAuth credentials**: `Checked`
    - **OAuth Client ID & Secret**: (Paste from Cloud Console)
3. **Step 1**: Search for `https://www.googleapis.com/auth/youtube` and click **Authorize APIs**.
4. **Step 2**: Click **Exchange authorization code for tokens**.
5. **Step 3**: Copy the `refresh_token` value from the JSON response to `appsettings.json`.

### D. Find your Persistent Stream ID
Because YouTube does not show this ID in the standard UI, use the OAuth Playground (Step 3) while still logged in:
1. Set **HTTP Method** to `GET`.
2. Paste this **Request URI**: `https://www.googleapis.com/youtube/v3/liveStreams?part=id,snippet&mine=true`
3. Click **Send Request**.
4. In the response JSON, find the `"id"` field (e.g., `"id": "YOUR_STREAM_ID"`).
5. Copy this value to **Administration > Livestream > Persistent Stream ID** in the app.
6. (Optional) Verify the `"title"` in the response matches your stream key name in YouTube Studio.

---

## 2. Facebook API Setup

> **Note:** The Facebook Graph API Explorer has a known bug where it automatically injects the deprecated `pages_show_list` scope, blocking token generation. The instructions below use a **System User Token** instead, which bypasses OAuth entirely, never expires, and is the correct approach for server-side apps.

### A. Prerequisites (Meta 2026 Policies)
- **Account Age**: The Facebook account must be at least **60 days old**.
- **Follower Count**: The Page must have at least **100 followers**.
- **Business Manager**: Your Page must be connected to a Meta Business Manager account.

### B. Persistent Stream Key & Auto-Start (One-Time Setup)
To ensure OBS never needs its settings changed and the stream starts automatically:
1. Go to your **Facebook Page > Professional Dashboard > Live Producer**.
2. Click **Stream Setup**.
3. Toggle ON **"Use a persistent stream key"**. Copy this into OBS.
4. Toggle ON **"Start as soon as the stream starts"** (or "Auto-start") in the dashboard settings.
5. Our API will handle creating the specific event session that "unlocks" this key for each broadcast.

### C. Create a Facebook App
1. Go to [Meta for Developers](https://developers.facebook.com/).
2. Create a new App: **Other → Business**.
3. Name it **Worship Console** → **Create App**.
4. From the app dashboard, click **Add a Use Case**.
5. Select **"Manage everything on your Page"** → **Save**.

### D. Create a System User
1. Go to [business.facebook.com](https://business.facebook.com) → **Settings → Users → System Users**.
2. Click **Add** → name it `WorshipConsole` → role: **Admin** → **Create System User**.

### E. Assign Assets to the System User
1. Click on **Worship Console Bot** → **Assign Assets**.
2. Select **Apps** → find **Worship Console** → toggle **Full Control** → **Save Changes**.
3. Click **Assign Assets** again → select **Pages** → find your church page → toggle **Full Control** → **Save Changes**.

### F. Generate the System User Token
1. Click **Generate New Token**.
2. Select the **Worship Console** app.
3. Set expiration to **Never**.
4. Check **`pages_manage_posts`** and **`pages_read_engagement`**.
5. Click **Generate Token**.
6. Copy the token to `appsettings.json` under `Facebook:PageAccessToken`.

### F2. Dev/Test-Only Flow (No App Review Yet)
If Facebook returns **`(#10)`** mentioning `live-video-api` review, follow this exact dev/test flow:

1. Keep your app in **Development** mode.
2. Ensure your **personal Facebook account** is an app role in **App Roles** (Administrator, Developer, or Tester).
3. Ensure that same personal account has **full control/admin** on the target Facebook Page.
4. In **Tools > Graph API Explorer** (`https://developers.facebook.com/tools/explorer/`), choose your app, then generate a **User Access Token** for your personal app-role account.
5. Include page scopes when generating the token (at minimum):
   - `pages_manage_posts`
   - `pages_read_engagement`
6. Exchange/inspect the token and obtain a **Page Access Token** for your target page (do not use a random user token directly for `/live_videos`).
7. Temporarily set that page token in `Facebook:PageAccessToken` and run these checks:
   - `GET /v22.0/{pageId}?fields=id,name`
   - `GET /v22.0/{pageId}/live_videos?limit=1`
8. If both checks succeed, scheduling should work in dev/test mode for role-based users.

> Important: A **System User** with business asset access can still hit `(#10)` for `live-video-api` in some setups until App Review is approved. For production/non-role usage, complete App Review.

### F3. Production Flow (Required for Real Public Use)
To schedule livestreams for users/pages beyond dev/test roles:

1. Complete **Business Verification** if Meta requires it.
2. Submit **App Review** for the Facebook features/permissions used by your livestream workflow.
3. Provide a working screencast and tester instructions during review.
4. After approval, retest with your production token and page.

### G. Find your Page ID
1. Go to your Facebook Page → About → Page Transparency.
2. Copy the numeric Page ID and put it in **Administration > Livestream**.

---

## 3. Thumbnails & Media
- **Resolution**: 1280x720 (16:9 ratio) is strictly recommended.
- **File Size**: Both platforms have a **2MB limit**.
- **Format**: Use `.jpg` or `.png`.

---

## 4. Application Configuration

### Non-Secrets (Administration Page)
Navigate to `/admin` and click the **Livestream** tab:
- **YouTube Stream ID**: The ID of your persistent stream.
- **Facebook Page ID**: Your Church's Page ID.
- **Default Start Time**: Usually `11:00`.

### Secrets (appsettings.json)
```json
"YouTube": {
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret",
  "RefreshToken": "your-refresh-token"
},
"Facebook": {
  "PageAccessToken": "your-page-access-token-or-system-user-token",
  "GraphApiVersion": "v22.0"
}
```

### Optional: Security Reminder
- Never commit real tokens/secrets to source control.
- If a secret is accidentally shared in screenshots, chat, or commits, rotate it immediately in Meta/Google.
