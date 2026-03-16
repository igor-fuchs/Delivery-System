using DeliverySystem.Application.Interfaces;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// BCrypt-based implementation of <see cref="IPasswordHasher"/>.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <inheritdoc />
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
