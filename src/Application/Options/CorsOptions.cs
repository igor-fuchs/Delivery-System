using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for CORS policy configuration.
/// Bound to the <c>Cors</c> configuration section.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Cors";

    /// <summary>Name of the registered CORS policy applied in the middleware pipeline.</summary>
    public const string PolicyName = "AuthCorsPolicy";

    /// <summary>Gets the list of origins permitted to make cross-origin requests.</summary>
    [Required]
    public required string[] AllowedOrigins { get; init; }

    /// <summary>Gets the list of HTTP methods allowed in cross-origin requests.</summary>
    [Required]
    public required string[] AllowedMethods { get; init; }
}
