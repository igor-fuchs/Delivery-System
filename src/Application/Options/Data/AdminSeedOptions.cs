using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for the admin seed user.
/// Bound to the <c>AdminSeed</c> configuration section.
/// </summary>
public sealed class AdminSeedOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AdminSeed";

    /// <summary>Gets the email address of the admin seed user.</summary>
    [Required]
    public required string Email { get; init; }

    /// <summary>Gets the password of the admin seed user.</summary>
    [Required]
    public required string Password { get; init; }
}
