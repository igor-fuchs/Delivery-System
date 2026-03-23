using System.Net;
using System.Text.Json;
using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.Presentation.Middlewares;

/// <summary>
/// Middleware that catches unhandled exceptions and converts them into structured JSON error responses.
/// Maps known exception types to appropriate HTTP status codes:
/// <list type="bullet">
///   <item><see cref="ValidationException"/> → 400 Bad Request</item>
///   <item><see cref="ConflictException"/> → 409 Conflict</item>
///   <item><see cref="UnauthorizedAccessException"/> → 401 Unauthorized</item>
///   <item><see cref="ServiceUnavailableException"/> → 503 Service Unavailable</item>
///   <item>All other exceptions → 500 Internal Server Error</item>
/// </list>
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, catching any exceptions that propagate from downstream.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Validation failed.", validationEx.Errors)
            ),
            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                new ErrorResponse(conflictEx.Message)
            ),
            UnauthorizedAccessException unauthorizedEx => (
                HttpStatusCode.Unauthorized,
                new ErrorResponse(unauthorizedEx.Message)
            ),
            ServiceUnavailableException serviceUnavailableEx => (
                HttpStatusCode.ServiceUnavailable,
                new ErrorResponse(serviceUnavailableEx.Message)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("An unexpected error occurred.")
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, JsonOptions.Default);
        await context.Response.WriteAsync(json);
    }

    private sealed record ErrorResponse(
        string Message,
        IReadOnlyDictionary<string, string[]>? Errors = null);

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}
