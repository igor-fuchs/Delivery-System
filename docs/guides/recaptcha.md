# reCAPTCHA v3 — Setup Guide

This project uses **Google reCAPTCHA v3** for bot protection on authentication endpoints.
reCAPTCHA v3 is invisible — it runs in the background and returns a score from `0.0` (bot) to `1.0` (human) without any user interaction.

---

## 1. Create a reCAPTCHA key pair

1. Go to [https://www.google.com/recaptcha/admin/create](https://www.google.com/recaptcha/admin/create)
2. Fill in the form:
   - **Label** — any name to identify the project (e.g. `DeliverySystem - Dev`)
   - **reCAPTCHA type** — select **Score based (v3)**
   - **Domains** — add your domain (e.g. `localhost` for local development)
3. Click **Submit**
4. Copy the **Secret Key** — you will need it in the next step

> ⚠️ Never commit the Secret Key to source control.

---

## 2. Configure the environment variable

Set the following environment variable in your environment (`.env`, server config, CI/CD secrets, etc.):

```
CAPTCHA__SECRET_KEY=your-captcha-secret-key
```

The double underscore (`__`) is the ASP.NET Core convention for nested configuration sections.
This maps to `RecaptchaOptions.SecretKey` in the application.

---

## 3. Configuration reference (`appsettings.json`)

The remaining options can be set in `appsettings.json` (non-sensitive values only):

```json
"Captcha": {
  "MinimumScore": 0.5,
  "SiteVerifyUrl": "https://www.google.com/recaptcha/api/siteverify"
}
```

| Property       | Description                                                                 | Default |
|----------------|-----------------------------------------------------------------------------|---------|
| `SecretKey`    | Google reCAPTCHA secret key. **Must come from environment variable.**       | —       |
| `MinimumScore` | Requests with a score below this threshold are rejected with HTTP 400.      | `0.5`   |
| `SiteVerifyUrl`| Google's verification endpoint. Should not need to change.                  | see above |

---

## 4. Frontend integration

Add the reCAPTCHA v3 script to your HTML and generate a token before each form submission:

```html
<script src="https://www.google.com/recaptcha/api.js?render=YOUR_SITE_KEY"></script>
```

```javascript
grecaptcha.ready(async () => {
  const token = await grecaptcha.execute('YOUR_SITE_KEY', { action: 'login' });
  // send token in the request body as `captchaToken`
});
```

> The **Site Key** (used on the frontend) is public and safe to expose.
> The **Secret Key** (used on the backend) must remain private.

---

## 5. How validation works

The backend validates the token on every protected endpoint:

1. The frontend generates a token via the Google JS SDK and sends it in the request
2. `RecaptchaService.ValidateAsync` posts the token to Google's `siteverify` API
3. Google returns a score and success flag
4. If the request fails, the score is below `MinimumScore`, or the response is invalid, a `ValidationException` is thrown and the request is rejected with **HTTP 400**

---

## 6. Local development

For local development, you can use a **test secret key** provided by Google that always returns success:

| Key type   | Value                                    |
|------------|------------------------------------------|
| Site key   | `6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI` |
| Secret key | `6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe` |

Set the test secret key as your `CAPTCHA__SECRET_KEY` environment variable while developing locally.

> These test keys will always pass validation regardless of the token sent.
