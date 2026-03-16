namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Response returned after a successful authentication (register or login).
/// </summary>
/// <param name="Id">The unique identifier of the authenticated user.</param>
/// <param name="Email">The email address of the authenticated user.</param>
/// <param name="Token">The JWT bearer token to use in subsequent requests.</param>
public sealed record AuthResponse(string Id, string Email, string Token);
