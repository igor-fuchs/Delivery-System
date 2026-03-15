using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
}
