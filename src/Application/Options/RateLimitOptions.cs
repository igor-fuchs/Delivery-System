using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for API rate limiting.
/// Bound to the <c>RateLimit</c> configuration section.
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Name of the authorization policy that applies the auth endpoint rate limiter.
    /// This policy should be used with [EnableRateLimiting] on auth-related controllers/actions.
    /// </summary>
    public const string AuthPolicyName = "Auth";

    /// <summary>
    /// Gets the maximum number of requests permitted per <see cref="AuthWindowMinutes"/> for the auth endpoints.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AuthPermitLimit { get; init; } = 10;

    /// <summary>
    /// Gets the sliding window duration in minutes for auth endpoint rate limiting.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AuthWindowMinutes { get; init; } = 30;

    /// <summary>
    /// Gets the maximum number of requests permitted per <see cref="GlobalWindowMinutes"/> for all endpoints (global limiter).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int GlobalPermitLimit { get; init; } = 30;

    /// <summary>
    /// Gets the sliding window duration in minutes for the global rate limiter.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int GlobalWindowMinutes { get; init; } = 10;
}
