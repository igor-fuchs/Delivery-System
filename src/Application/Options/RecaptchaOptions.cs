using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Strongly-typed options for Google reCAPTCHA verification.
/// Bound to the <c>Recaptcha</c> configuration section.
/// </summary>
public sealed class RecaptchaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Recaptcha";

    /// <summary>Gets the reCAPTCHA secret key used for server-side token verification.</summary>
    [Required]
    public required string SecretKey { get; init; }

    /// <summary>Gets the minimum score threshold (0.0–1.0) for reCAPTCHA v3. Tokens below this score are rejected.</summary>
    [Range(0.0, 1.0)]
    public double MinimumScore { get; init; } = 0.5;
}
