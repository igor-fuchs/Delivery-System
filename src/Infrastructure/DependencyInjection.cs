using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Infrastructure.Identity;
using DeliverySystem.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

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
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
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

        // Redis
        services
            .AddOptions<RedisOptions>()
            .BindConfiguration(RedisOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(opts.ConnectionString);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            var redisOpts = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()!;
            options.Configuration = redisOpts.ConnectionString;
            options.InstanceName = redisOpts.InstanceName;
        });

        // Services
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ProductService>();
        services.AddScoped<IProductService>(sp =>
            new CachedProductService(
                sp.GetRequiredService<ProductService>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(),
                sp.GetRequiredService<IOptions<RedisOptions>>()));

        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<DatabaseSeeder>();

        services
            .AddOptions<AdminSeedOptions>()
            .BindConfiguration(AdminSeedOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<GoogleOptions>()
            .BindConfiguration(GoogleOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (environment.IsDevelopment())
        {
            services.AddSingleton<ICaptchaService, FakeCaptchaService>();
        }
        else
        {
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
        }

        return services;
    }
}
