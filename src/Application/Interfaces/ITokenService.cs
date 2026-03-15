using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
