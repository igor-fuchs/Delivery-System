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
    /// Name of the rate limiting policy applied to auth endpoints.
    /// </summary>
    public const string AuthPolicyName = "Auth";

    /// <summary>
    /// Name of the rate limiting policy applied to product endpoints.
    /// </summary>
    public const string ProductsPolicyName = "Products";

    /// <summary>
    /// Gets the maximum number of requests permitted per <see cref="GlobalWindowMinutes"/> for all endpoints (global limiter).
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int GlobalPermitLimit { get; init; }

    /// <summary>
    /// Gets the sliding window duration in minutes for the global rate limiter.
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int GlobalWindowMinutes { get; init; }

    /// <summary>
    /// Gets the maximum number of requests permitted per <see cref="AuthWindowMinutes"/> for the auth endpoints.
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int AuthPermitLimit { get; init; }

    /// <summary>
    /// Gets the sliding window duration in minutes for auth endpoint rate limiting.
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int AuthWindowMinutes { get; init; }

    /// <summary>
    /// Gets the maximum number of requests permitted per <see cref="ProductsWindowMinutes"/> for product endpoints, per IP address.
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int ProductsPermitLimit { get; init; }

    /// <summary>
    /// Gets the window duration in minutes for the products rate limiter.
    /// </summary>
    [Range(1, int.MaxValue)]
    public required int ProductsWindowMinutes { get; init; }
}
