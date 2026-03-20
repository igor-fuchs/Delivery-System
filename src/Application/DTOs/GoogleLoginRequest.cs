namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for Google OAuth2 login.
/// The client (web or mobile) obtains a Google ID token via Google Sign-In
/// and sends it to this endpoint for server-side validation.
/// </summary>
/// <param name="IdToken">The Google ID token obtained from the Google Sign-In SDK.</param>
public sealed record GoogleLoginRequest(string IdToken);
