namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for new user registration.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plaintext password (must meet complexity rules).</param>
/// <param name="CaptchaToken">The reCAPTCHA token obtained from the client.</param>
public sealed record RegisterRequest(string Email, string Password, string CaptchaToken);
