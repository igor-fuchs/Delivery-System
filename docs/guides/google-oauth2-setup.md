# Google OAuth 2.0 — Credentials Setup

This guide describes how to create OAuth 2.0 credentials in the Google Cloud Console for **web (desktop)** and **Android (mobile)** clients, and how to configure them in the `.env` file.

---

## Prerequisites

- A Google account with access to [Google Cloud Console](https://console.cloud.google.com/)
- An existing Google Cloud project, or permission to create one

---

## 1. Create or Select a Google Cloud Project

1. Go to [console.cloud.google.com](https://console.cloud.google.com/)
2. Click the project selector (top-left) and choose **"New Project"**
3. Set a name (e.g. `delivery-system`) and click **"Create"**
4. Wait for the project to be created and make sure it is selected

---

## 2. Configure the OAuth Consent Screen

Before creating credentials, you must configure the consent screen that users will see when signing in.

1. In the side menu, go to **APIs & Services → OAuth consent screen**
2. Select the user type:
   - **Internal** — only users from your Google Workspace organization
   - **External** — any Google account (recommended for public apps)
3. Click **"Create"** and fill in the required fields:
   - **App name**: `Delivery System`
   - **User support email**: your email
   - **Developer contact email**: your email
4. Click **"Save and continue"** through the remaining steps (default scopes are sufficient)

---

## 3. Create a Web / Desktop Credential

The web client is used by the API to validate ID tokens sent by any client (web, desktop, Postman, etc.).

1. Go to **APIs & Services → Credentials**
2. Click **"+ Create Credentials" → "OAuth client ID"**
3. Under **Application type**, select **"Web application"**
4. Fill in the fields:
   - **Name**: `Delivery System - Web`
   - **Authorized JavaScript origins**: add your front-end origins (e.g. `http://localhost:3000`, `https://yourdomain.com`)
   - **Authorized redirect URIs**: add the callback URI if using the Authorization Code Flow (e.g. `http://localhost:3000/auth/callback`). For server-side ID token validation, this field can be left empty.
5. Click **"Create"**
6. Copy the **"Client ID"** — it will have the format `XXXXXXXXXX.apps.googleusercontent.com`

---

## 4. Create an Android (Mobile) Credential

The Android client generates tokens that are also validated by the API.

1. On the same **Credentials** screen, click **"+ Create Credentials" → "OAuth client ID"**
2. Under **Application type**, select **"Android"**
3. Fill in the fields:
   - **Name**: `Delivery System - Android`
   - **Package name**: your Android app package name (e.g. `com.yourcompany.deliverysystem`)
   - **SHA-1 certificate fingerprint**: obtain it with the command below

```bash
# Debug keystore (development)
keytool -keystore ~/.android/debug.keystore -list -v -alias androiddebugkey -storepass android -keypass android

# Release keystore (production)
keytool -keystore path/to/your-release.keystore -list -v
```

4. Copy the `SHA1:` line from the output and paste it in the field
5. Click **"Create"**
6. Copy the generated **"Client ID"** — it also has the format `XXXXXXXXXX.apps.googleusercontent.com`

> **Note:** Android credentials have no client secret. Validation is done via SHA-1 fingerprint + package name.

---

## 5. Configure the `.env` File

Open the `.env` file at the project root and add (or update) the following variables:

```env
# --- Google OAuth2 ------------------------------------------
GOOGLE__WEBCLIENTID=<your-web-client-id>.apps.googleusercontent.com
```

> If Android support (`AndroidClientId`) is enabled in `GoogleOptions.cs`, also add:
>
> ```env
> GOOGLE__ANDROIDCLIENTID=<your-android-client-id>.apps.googleusercontent.com
> ```

### Filled example

```env
# --- Google OAuth2 ------------------------------------------
GOOGLE__WEBCLIENTID=123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com
```

---

## 6. Configuration Mapping

Environment variables are mapped to the `GoogleOptions` class using .NET's `SECTION__PROPERTY` convention:

| `.env` Variable | C# Property |
|---|---|
| `GOOGLE__WEBCLIENTID` | `GoogleOptions.WebClientId` |
| `GOOGLE__ANDROIDCLIENTID` | `GoogleOptions.AndroidClientId` |

The configuration section is defined by the constant `GoogleOptions.SectionName = "Google"`.

---

## 7. Test the Integration

With the API running locally, send a valid Google ID token to the authentication endpoint:

```http
POST /api/auth/google
Content-Type: application/json

{
  "idToken": "<google-id-token-obtained-on-the-client>"
}
```

A `200 OK` response with the application JWT confirms the integration is working.

---

## References

- [Google Identity: Verify the Google ID token on your server-side](https://developers.google.com/identity/gsi/web/guides/verify-google-id-token)
- [Google Cloud Console — Credentials](https://console.cloud.google.com/apis/credentials)
- [Google Sign-In for Android](https://developers.google.com/identity/sign-in/android/start-integrating)
