using DeliverySystem.Domain.Exceptions;

namespace DeliverySystem.Domain.Entities;

/// <summary>
/// Represents a user in the delivery system.
/// </summary>
public sealed class User
{
    /// <summary>Gets the unique identifier of the user.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the full name of the user.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the email address of the user.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the BCrypt-hashed password.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the user was created.</summary>
    public DateTime CreatedAt { get; private set; }

    private User() { }

    /// <summary>
    /// Creates a new <see cref="User"/> instance with the specified data.
    /// </summary>
    /// <param name="name">The user's full name. Must not be empty.</param>
    /// <param name="email">The user's email address. Must not be empty.</param>
    /// <param name="passwordHash">The pre-hashed password. Must not be empty.</param>
    /// <returns>A new <see cref="User"/> entity.</returns>
    /// <exception cref="DomainException">Thrown when any of the required parameters are empty or whitespace.</exception>
    public static User Create(string name, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Nome não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("E-mail não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Hash de senha não pode ser vazio.");

        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }
}
