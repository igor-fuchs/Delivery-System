using System.Threading.RateLimiting;
using DeliverySystem.Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedisRateLimiting;
using StackExchange.Redis;

namespace DeliverySystem.Presentation.Extensions;

/// <summary>
/// Extension methods for configuring ASP.NET Core rate limiting.
/// Uses Redis as the backing store when <see cref="IConnectionMultiplexer"/> is registered;
/// falls back to in-memory limiters otherwise (e.g. during integration tests).
/// </summary>
public static class RateLimiterExtensions
{
    /// <summary>
    /// Registers rate limiting services with IP-based global limits and a stricter
    /// named policy for authentication endpoints.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="RateLimitOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddAuthRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RateLimitOptions>()
            .BindConfiguration(RateLimitOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var opts = configuration
            .GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>()!;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Named policy applied to auth endpoints via [EnableRateLimiting(RateLimitOptions.AuthPolicyName)]
            options.AddPolicy(RateLimitOptions.AuthPolicyName, context =>
            {
                var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var multiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();

                if (multiplexer is not null)
                {
                    return RedisRateLimitPartition.GetFixedWindowRateLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new RedisFixedWindowRateLimiterOptions
                        {
                            PermitLimit = opts.AuthPermitLimit,
                            Window = TimeSpan.FromMinutes(opts.AuthWindowMinutes),
                            ConnectionMultiplexerFactory = () => multiplexer
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.AuthPermitLimit,
                        Window = TimeSpan.FromMinutes(opts.AuthWindowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Global IP-based limiter applied to every request
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var multiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();

                if (multiplexer is not null)
                {
                    return RedisRateLimitPartition.GetFixedWindowRateLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new RedisFixedWindowRateLimiterOptions
                        {
                            PermitLimit = opts.GlobalPermitLimit,
                            Window = TimeSpan.FromMinutes(opts.GlobalWindowMinutes),
                            ConnectionMultiplexerFactory = () => multiplexer
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.GlobalPermitLimit,
                        Window = TimeSpan.FromMinutes(opts.GlobalWindowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
