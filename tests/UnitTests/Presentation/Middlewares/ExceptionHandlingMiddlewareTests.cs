using System.Net;
using System.Text.Json;
using DeliverySystem.Application.Constants;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DeliverySystem.UnitTests.Presentation.Middlewares;

public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger =
        Substitute.For<ILogger<ExceptionHandlingMiddleware>>();

    /// <summary>
    /// Runs the middleware with a delegate that throws the given exception
    /// and returns the HTTP response context for assertions.
    /// </summary>
    private async Task<HttpContext> InvokeMiddlewareAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, _logger);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ShouldReturn400WithErrorCode()
    {
        var errors = new Dictionary<string, ValidationFieldError[]>
        {
            ["Email"] = [new ValidationFieldError(ErrorCodes.ValidationFailed, "Email is required.")]
        };

        var context = await InvokeMiddlewareAsync(new ValidationException(errors));

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var body = await DeserializeResponseAsync(context);
        Assert.Equal("Validation failed.", body.GetProperty("message").GetString());
        Assert.Equal(ErrorCodes.ValidationFailed, body.GetProperty("errorCode").GetString());
        Assert.True(body.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task InvokeAsync_ConflictException_ShouldReturn409WithErrorCode()
    {
        var context = await InvokeMiddlewareAsync(new ConflictException("User already exists.", ErrorCodes.UserAlreadyExists));

        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);

        var body = await DeserializeResponseAsync(context);
        Assert.Equal("User already exists.", body.GetProperty("message").GetString());
        Assert.Equal(ErrorCodes.UserAlreadyExists, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_AppUnauthorizedException_ShouldReturn401WithErrorCode()
    {
        var context = await InvokeMiddlewareAsync(new AppUnauthorizedException("Invalid credentials.", ErrorCodes.InvalidCredentials));

        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);

        var body = await DeserializeResponseAsync(context);
        Assert.Equal("Invalid credentials.", body.GetProperty("message").GetString());
        Assert.Equal(ErrorCodes.InvalidCredentials, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_GenericException_ShouldReturn500WithErrorCode()
    {
        var context = await InvokeMiddlewareAsync(new InvalidOperationException("Something broke"));

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        var body = await DeserializeResponseAsync(context);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("message").GetString());
        Assert.Equal(ErrorCodes.InternalError, body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InvokeAsync_GenericException_ShouldNotLeakDetails()
    {
        var context = await InvokeMiddlewareAsync(new InvalidOperationException("sensitive-info"));

        var body = await DeserializeResponseAsync(context);

        // The original exception message must not appear in the response.
        Assert.DoesNotContain("sensitive-info", body.GetRawText());
    }

    [Fact]
    public async Task InvokeAsync_NoException_ShouldPassThrough()
    {
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask, _logger);

        await middleware.InvokeAsync(context);

        // Status code remains 200 (default) when no exception is thrown.
        Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
    }

    private static async Task<JsonElement> DeserializeResponseAsync(HttpContext context)
    {
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
