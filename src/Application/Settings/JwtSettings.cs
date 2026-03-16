using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Settings;

/// <summary>
/// Strongly-typed configuration for JWT authentication.
/// Bound to the <c>Jwt</c> configuration section.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Gets the symmetric secret key used to sign and verify tokens.</summary>
    [Required]
    public required string SecretKey { get; init; }

    /// <summary>Gets the issuer claim (<c>iss</c>) embedded in generated tokens.</summary>
    [Required]
    public required string Issuer { get; init; }

    /// <summary>Gets the audience claim (<c>aud</c>) embedded in generated tokens.</summary>
    [Required]
    public required string Audience { get; init; }

    /// <summary>Gets the token lifetime in minutes.</summary>
    [Required]
    public required int ExpirationMinutes { get; init; }
}
