namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for user login.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plaintext password.</param>
public sealed record LoginRequest(string Email, string Password);
