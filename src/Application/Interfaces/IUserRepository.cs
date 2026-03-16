using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Repository abstraction for <see cref="User"/> persistence.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address to search for (case-insensitive).</param>
    /// <returns>The matching <see cref="User"/>, or <c>null</c> if not found.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Persists a new user.
    /// </summary>
    /// <param name="user">The user entity to store.</param>
    Task AddAsync(User user);
}
