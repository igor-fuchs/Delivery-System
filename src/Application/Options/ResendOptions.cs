using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for the Resend email delivery service.
/// Bound to the <c>Resend</c> configuration section.
/// </summary>
public sealed class ResendOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Resend";

    /// <summary>Gets the Resend API key used for authentication.</summary>
    [Required]
    public required string ApiKey { get; init; }

    /// <summary>Gets the verified sender email address (e.g. <c>noreply@yourdomain.com</c>).</summary>
    [Required]
    public required string FromEmail { get; init; }
}
