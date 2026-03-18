using DeliverySystem.Application.DTOs;

namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for authentication operations (registration and login).
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
    /// <exception cref="UnauthorizedAccessException">Thrown when the email is not found or the password is incorrect.</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
