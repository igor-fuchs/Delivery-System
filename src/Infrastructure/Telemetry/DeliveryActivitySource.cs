using System.Diagnostics;

namespace DeliverySystem.Infrastructure.Telemetry;

/// <summary>
/// Central <see cref="ActivitySource"/> for custom telemetry spans emitted by the Infrastructure layer.
/// Register its <see cref="Name"/> with <c>AddSource()</c> in the OpenTelemetry tracing builder.
/// </summary>
public static class DeliveryActivitySource
{
    /// <summary>
    /// The source name used to register this <see cref="ActivitySource"/> with the OpenTelemetry SDK.
    /// </summary>
    public const string Name = "DeliverySystem.Infrastructure";

    /// <summary>
    /// The shared <see cref="ActivitySource"/> instance for creating custom spans in Infrastructure services.
    /// </summary>
    public static readonly ActivitySource Instance = new(Name, "1.0.0");
}
