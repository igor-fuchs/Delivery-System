using System.ComponentModel.DataAnnotations;

namespace DeliverySystem.Application.Options;

/// <summary>
/// Configuration options for OpenTelemetry SDK.
/// Binds from the "OpenTelemetry" configuration section.
/// </summary>
public sealed class OpenTelemetryOptions
{
    /// <summary>
    /// Configuration section name for binding OpenTelemetry options.
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// The OTLP gRPC endpoint where the OpenTelemetry SDK exports traces, metrics, and logs.
    /// Must be a valid URI (e.g., "http://otel-collector:4317" or "http://localhost:4317").
    /// </summary>
    [Required]
    [Url]
    public required string OtlpEndpoint { get; init; }
}
