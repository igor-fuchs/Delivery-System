using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DeliverySystem.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all infrastructure services (EF Core, Identity, token service, and auth service)
    /// to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration for reading connection strings.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(dbOptions.ConnectionString);
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        services
            .AddOptions<GoogleOptions>()
            .BindConfiguration(GoogleOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<RecaptchaOptions>()
            .BindConfiguration(RecaptchaOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ICaptchaService, RecaptchaService>(client =>
        {
            client.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
