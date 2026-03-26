using System.Net;
using System.Text.Json;
using DeliverySystem.Application.Constants;
using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.Presentation.Middlewares;

/// <summary>
/// Middleware that catches unhandled exceptions and converts them into structured JSON error responses.
/// Every response includes a machine-readable <c>errorCode</c> for front-end i18n.
/// Maps known exception types to appropriate HTTP status codes:
/// <list type="bullet">
///   <item><see cref="ValidationException"/> → 400 Bad Request</item>
///   <item><see cref="ConflictException"/> → 409 Conflict</item>
///   <item><see cref="AppUnauthorizedException"/> → 401 Unauthorized</item>
///   <item><see cref="NotFoundException"/> → 404 Not Found</item>
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
                new ErrorResponse("Validation failed.", ErrorCodes.ValidationFailed, validationEx.Errors)
            ),
            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                new ErrorResponse(conflictEx.Message, conflictEx.Code)
            ),
            AppUnauthorizedException unauthorizedEx => (
                HttpStatusCode.Unauthorized,
                new ErrorResponse(unauthorizedEx.Message, unauthorizedEx.Code)
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse(notFoundEx.Message, notFoundEx.Code)
            ),
            ServiceUnavailableException serviceUnavailableEx => (
                HttpStatusCode.ServiceUnavailable,
                new ErrorResponse(serviceUnavailableEx.Message, serviceUnavailableEx.Code)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("An unexpected error occurred.", ErrorCodes.InternalError)
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
        string ErrorCode,
        IReadOnlyDictionary<string, ValidationFieldError[]>? Errors = null);

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}
