using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Telemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DeliverySystem.Presentation.Extensions;

/// <summary>
/// Extension methods for registering OpenTelemetry tracing, metrics, and logging
/// with OTLP export to the OpenTelemetry Collector.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registers the OpenTelemetry SDK with traces, metrics, and a log bridge,
    /// all exported via OTLP gRPC to the configured collector endpoint.
    /// W3C Trace Context propagation is enabled by default via the AspNetCore instrumentation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration used to read the OTLP endpoint.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OpenTelemetryOptions>()
            .BindConfiguration(OpenTelemetryOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var otlpOptions = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>()!;

        var otlpEndpoint = otlpOptions.OtlpEndpoint;
        var serviceName = "delivery-system-api";
        var serviceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version!.ToString();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    // W3C TraceContext is the default propagator — no extra config needed.
                    opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    // Record exception type and stack trace on the span — no PII risk.
                    opts.RecordException = true;
                })
                .AddHttpClientInstrumentation(opts =>
                {
                    // Override the full URI tag to only record the host, preventing query strings
                    // (which may contain tokens or secrets) from appearing in traces.
                    opts.EnrichWithHttpRequestMessage = (activity, req) =>
                        activity.SetTag("http.server.address", req.RequestUri?.Host);
                })
                .AddEntityFrameworkCoreInstrumentation(opts =>
                {
                    // Do NOT capture DB statement text — parameterized values could contain PII.
                    opts.SetDbStatementForText = false;
                    opts.SetDbStatementForStoredProcedure = false;
                })
                .AddRedisInstrumentation(opts =>
                {
                    // Do not capture Redis command values — keys may contain session tokens or PII.
                    opts.SetVerboseDatabaseStatements = false;
                })
                .AddSource(DeliveryActivitySource.Name)
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(otlpEndpoint);
                    opts.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(otlpEndpoint);
                    opts.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithLogging(logging => logging
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(otlpEndpoint);
                    opts.Protocol = OtlpExportProtocol.Grpc;
                }));

        return services;
    }
}
