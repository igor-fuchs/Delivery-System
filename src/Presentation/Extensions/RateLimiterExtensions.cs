using System.Threading.RateLimiting;
using DeliverySystem.Application.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DeliverySystem.Presentation.Extensions;

/// <summary>
/// Extension methods for configuring ASP.NET Core rate limiting.
/// </summary>
public static class RateLimiterExtensions
{
    /// <summary>
    /// Registers rate limiting services with IP-based global limits and a stricter named policy
    /// for authentication endpoints.
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
            options.AddFixedWindowLimiter(RateLimitOptions.AuthPolicyName, limiter =>
            {
                limiter.PermitLimit = opts.AuthPermitLimit;
                limiter.Window = TimeSpan.FromMinutes(opts.AuthWindowMinutes);
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiter.QueueLimit = 0;
            });

            // Custom response for rejected requests
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Too many requests. Please try again later."
                }, cancellationToken);
            };

            // Global IP-based limiter applied to every request
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: _.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: __ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.GlobalPermitLimit,
                        Window = TimeSpan.FromMinutes(opts.GlobalWindowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }
                )
            );

        });

        return services;
    }
}
