using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Infrastructure.Identity;
using DeliverySystem.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Resend;
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
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Redis — abortConnect=false prevents startup failures when Redis is temporarily
        // unavailable (e.g. during integration tests or delayed container startup).
        services
            .AddOptions<RedisOptions>()
            .BindConfiguration(RedisOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var redisOpts = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()!;
        var redisConfig = ConfigurationOptions.Parse(redisOpts.ConnectionString);
        redisConfig.AbortOnConnectFail = false;
        var multiplexer = ConnectionMultiplexer.Connect(redisConfig);

        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer);
            options.InstanceName = redisOpts.InstanceName;
        });

        // Persist Data Protection keys to Redis so they survive container restarts.
        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(multiplexer, "DataProtection-Keys")
            .SetApplicationName("DeliverySystem");

        // Services
        services.AddSingleton<ICleanerService, CleanerService>();
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

        // Resend email service
        services
            .AddOptions<ResendOptions>()
            .BindConfiguration(ResendOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o => o.ApiToken = configuration[$"{ResendOptions.SectionName}:ApiKey"]!);
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailService, ResendEmailService>();

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
