namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for Google OAuth2 login.
/// The client (web or mobile) authenticates with Google and sends the resulting ID token.
/// </summary>
/// <param name="IdToken">The Google ID token obtained from the client-side OAuth2 flow.</param>
public sealed record GoogleLoginRequest(string IdToken);
