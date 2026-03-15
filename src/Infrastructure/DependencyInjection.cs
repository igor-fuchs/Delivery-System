using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Services;
using DeliverySystem.Infrastructure.Repositories;
using DeliverySystem.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeliverySystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<AuthService>();

        return services;
    }
}
