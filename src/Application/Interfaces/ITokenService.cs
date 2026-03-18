namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for JWT token generation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="email">The user's email address.</param>
    /// <returns>A signed JWT string.</returns>
    string GenerateToken(Guid userId, string email);
}
