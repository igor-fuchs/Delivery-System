using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Settings;

public sealed class JwtSettings
{
    [Required]
    public required string SecretKey { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Required]
    public required int ExpirationMinutes { get; init; }
}
