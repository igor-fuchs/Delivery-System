namespace DeliverySystem.Presentation.Middlewares;

/// <summary>
/// Middleware that logs HTTP request metadata (method, route template, status code, duration)
/// for every request. Uses the route template instead of the actual path to avoid capturing
/// user or entity IDs embedded in path segments (PII risk). Request bodies, query strings,
/// and headers are never logged.
/// </summary>
public sealed class RequestTracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTracingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTracingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for request tracing events.</param>
    public RequestTracingMiddleware(RequestDelegate next, ILogger<RequestTracingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, measures the request duration, and logs the result.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            // Use route template (e.g. /api/orders/{id}) rather than the actual path
            // (e.g. /api/orders/3fa85f64-...) to prevent GUIDs from appearing in every log line.
            var routePattern = (context.GetEndpoint() as RouteEndpoint)
                ?.RoutePattern.RawText ?? context.Request.Path.Value;

            _logger.LogInformation(
                "HTTP {Method} {RoutePattern} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                routePattern,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
