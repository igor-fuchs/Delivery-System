namespace DeliverySystem.Application.Settings;

public sealed class JwtSettings
{
    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int ExpirationMinutes { get; init; }
}
