# Resend — Setup Guide

This project uses **[Resend](https://resend.com)** as the transactional email provider to send password reset emails. In Development mode, emails are not sent — the reset URL is logged to the console instead.

---

## 1. Create a Resend account

1. Go to [resend.com](https://resend.com) and sign up for a free account
2. Verify your email address to activate the account

---

## 2. Add and verify a sending domain

Resend requires a verified domain to send emails in production. The free plan also allows sending to your own email using the shared `onboarding@resend.dev` address for testing.

### Option A — Use the shared test address (development only)

Skip domain setup and use `onboarding@resend.dev` as the `FROM_EMAIL`. This works out of the box but only delivers emails to the account owner's address.

### Option B — Add your own domain (recommended for production)

1. In the Resend dashboard, go to **Domains → Add Domain**
2. Enter your domain (e.g. `yourdomain.com`)
3. Resend will provide DNS records to add to your domain registrar:
   - `SPF` — authorizes Resend to send on your domain's behalf
   - `DKIM` — signs outgoing emails for authenticity
   - `DMARC` — (recommended) defines email authentication policy
4. Add the records at your DNS provider and click **Verify**
5. Wait for propagation (usually a few minutes, up to 48 hours)

Once verified, you can use any address on that domain as the sender (e.g. `noreply@yourdomain.com`).

---

## 3. Generate an API key

1. In the Resend dashboard, go to **API Keys → Create API Key**
2. Set a name (e.g. `delivery-system-prod`)
3. Set the **Permission** to **Sending access**
4. Optionally restrict the key to a specific verified domain
5. Click **Add** and copy the key — it will only be shown once

> Never commit the API key to source control.

---

## 4. Configure the environment variables

Set the following variables in your `.env` file:

```env
# --- Resend --------------------------------------------------
RESEND__API_KEY=re_your_api_key_here
RESEND__FROM_EMAIL=noreply@yourdomain.com
```

| Variable | Description |
|---|---|
| `RESEND__API_KEY` | The API key generated in step 3 |
| `RESEND__FROM_EMAIL` | The sender address shown to recipients (must be on a verified domain) |

The double underscore (`__`) is the ASP.NET Core convention for nested sections, mapping to `ResendOptions.ApiKey` and `ResendOptions.FromEmail`.

---

## 5. How it works in the project

The email integration is environment-aware:

| Environment | Implementation | Behavior |
|---|---|---|
| `Development` | `FakeEmailService` | Logs the reset URL to the console — no email sent |
| `Production` | `ResendEmailService` | Sends a styled HTML email via `POST https://api.resend.com/emails` |
| Tests | `TestFakeEmailService` | Captures sent emails in memory for assertions |

The email is triggered by `POST /api/auth/forgot-password`. If the Resend API returns a non-2xx response, the endpoint returns **503 Service Unavailable** with error code `EMAIL_DELIVERY_FAILED`.

---

## 6. Local development

No Resend account is needed for local development. When `ASPNETCORE_ENVIRONMENT=Development`, the `FakeEmailService` is used automatically and the reset URL is printed to the application logs:

```
[FakeEmailService] Password reset link for user@example.com:
https://yourfrontend.com/reset-password?userId=...&token=...
```

Copy that URL and use it to test the reset flow end-to-end without sending any real email.
