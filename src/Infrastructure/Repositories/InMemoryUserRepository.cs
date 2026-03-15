using System.Collections.Concurrent;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Infrastructure.Repositories;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    public Task<User?> GetByEmailAsync(string email)
    {
        _users.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _users[user.Email] = user;
        return Task.CompletedTask;
    }
}
