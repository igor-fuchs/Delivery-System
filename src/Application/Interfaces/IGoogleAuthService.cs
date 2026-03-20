using DeliverySystem.Application.DTOs;

namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for Google OAuth2 authentication operations.
/// Validates a Google ID token, provisions the user if needed, and returns a JWT.
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Authenticates a user via a Google ID token.
    /// If the user does not exist, a new account is created and linked to Google.
    /// </summary>
    /// <param name="request">The request containing the Google ID token.</param>
    /// <returns>An <see cref="AuthResponse"/> containing the user ID, email, and JWT token.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the Google ID token is invalid or expired.</exception>
    Task<AuthResponse> LoginAsync(GoogleLoginRequest request);
}
