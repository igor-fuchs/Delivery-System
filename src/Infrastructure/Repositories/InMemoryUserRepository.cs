using System.Collections.Concurrent;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of <see cref="IUserRepository"/>.
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by email (case-insensitive).
/// Intended as a placeholder until a persistent store (e.g. EF Core) is configured.
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(string email)
    {
        _users.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    /// <inheritdoc />
    public Task AddAsync(User user)
    {
        _users[user.Email] = user;
        return Task.CompletedTask;
    }
}
