using DeliverySystem.Application.DTOs;

namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for authentication operations (registration, login, and external provider login).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user and returns a JWT token.
    /// </summary>
    /// <param name="request">The registration data.</param>
    /// <returns>An <see cref="AuthResponse"/> containing the user ID, email, and JWT token.</returns>
    /// <exception cref="Exceptions.ConflictException">Thrown when a user with the same email already exists.</exception>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user by email and password, and returns a JWT on success.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    /// <returns>An <see cref="AuthResponse"/> containing the user ID, email, and JWT token.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when no account exists for the supplied email address.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the password is incorrect or CAPTCHA verification fails.</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Authenticates a user via a Google ID token. Creates the user if they don't already exist.
    /// Supports both web and mobile clients.
    /// </summary>
    /// <param name="request">The Google login data containing the ID token.</param>
    /// <returns>An <see cref="AuthResponse"/> containing the user ID, email, and JWT token.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the Google ID token is invalid or expired.</exception>
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);
}
