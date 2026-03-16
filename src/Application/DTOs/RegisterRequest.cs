namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for new user registration.
/// </summary>
/// <param name="Name">The user's full name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plaintext password (must meet complexity rules).</param>
public sealed record RegisterRequest(string Name, string Email, string Password);
