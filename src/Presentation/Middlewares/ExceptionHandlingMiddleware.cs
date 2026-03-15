using System.Net;
using System.Text.Json;
using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.Presentation.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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
