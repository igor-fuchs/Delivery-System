using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for Google OAuth2 authentication.
/// Bound to the <c>Google</c> configuration section.
/// Supports both web and mobile clients by accepting multiple audience client IDs.
/// </summary>
public sealed class GoogleOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Google";

    /// <summary>Gets the Google OAuth2 client ID for web applications.</summary>
    [Required]
    public required string WebClientId { get; init; }

    /// <summary>Gets the Google OAuth2 client ID for mobile (Android/iOS) applications.</summary>
    [Required]
    public required string MobileClientId { get; init; }
}
