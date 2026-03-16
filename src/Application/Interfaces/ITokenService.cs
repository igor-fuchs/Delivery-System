using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for JWT token generation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a signed JWT for the specified user.
    /// </summary>
    /// <param name="user">The user whose claims will be embedded in the token.</param>
    /// <returns>A signed JWT string.</returns>
    string GenerateToken(User user);
}
