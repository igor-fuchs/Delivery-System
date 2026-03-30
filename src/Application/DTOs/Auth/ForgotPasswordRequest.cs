namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for initiating a password reset.
/// </summary>
/// <param name="Email">The email address of the account to reset.</param>
/// <param name="CaptchaToken">The reCAPTCHA token obtained from the client.</param>
/// <param name="CallbackUrl">
/// The frontend base URL for the reset page (e.g. <c>https://app.example.com/reset-password</c>).
/// The backend appends <c>?userId=…&amp;token=…</c> to construct the full reset link sent by email.
/// </param>
public sealed record ForgotPasswordRequest(string Email, string CaptchaToken, string CallbackUrl);
