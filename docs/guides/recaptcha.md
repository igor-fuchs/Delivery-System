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

> Never commit the Secret Key to source control.

---

## 2. Configure the environment variable

Set the following environment variable in your `.env` file or CI/CD secrets:

```env
RECAPTCHA__SECRET_KEY=your-captcha-secret-key
```

The double underscore (`__`) is the ASP.NET Core convention for nested configuration sections, mapping to `RecaptchaOptions.SecretKey`.

---

## 3. Configuration reference

The `MinimumScore` threshold can be adjusted in `appsettings.json`:

```json
"Recaptcha": {
  "MinimumScore": 0.5
}
```

| Property | Description | Default |
|---|---|---|
| `SecretKey` | Google reCAPTCHA secret key. **Must come from the environment variable.** | — |
| `MinimumScore` | Requests with a score below this threshold are rejected with HTTP 401. | `0.5` |

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

The `captchaToken` field is required in the request body for: `POST /api/auth/register`, `POST /api/auth/login`, and `POST /api/auth/forgot-password`.

---

## 5. How validation works

1. The frontend generates a token via the Google JS SDK and sends it in the `captchaToken` field
2. `RecaptchaService.ValidateAsync` posts the token to Google's `siteverify` API
3. Google returns a score and a success flag
4. If the request fails, the score is below `MinimumScore`, or the response is invalid, `AuthService` throws an `AppUnauthorizedException` and the request is rejected with **HTTP 401**

---

## 6. Local development

For local development, use the **test keys** provided by Google that always return success:

| Key type | Value |
|---|---|
| Site key | `6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI` |
| Secret key | `6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe` |

Set the test secret key as your `RECAPTCHA__SECRET_KEY` environment variable while developing locally.

> These test keys will always pass validation regardless of the token sent.
