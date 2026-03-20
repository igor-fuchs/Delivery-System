using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for Google OAuth2 authentication.
/// Bound to the <c>Google</c> configuration section.
/// </summary>
public sealed class GoogleOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Google";

    /// <summary>Gets the Google OAuth2 client ID used to validate ID tokens.</summary>
    [Required]
    public required string ClientId { get; init; }
}
