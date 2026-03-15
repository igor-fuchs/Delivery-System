using DeliverySystem.Domain.Exceptions;

namespace DeliverySystem.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private User() { }

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
