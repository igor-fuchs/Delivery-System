namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for user login.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plaintext password.</param>
/// <param name="CaptchaToken">The reCAPTCHA token obtained from the client.</param>
public sealed record LoginRequest(string Email, string Password, string CaptchaToken);
