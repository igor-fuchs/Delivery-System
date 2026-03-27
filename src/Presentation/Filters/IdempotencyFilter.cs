using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace DeliverySystem.Presentation.Filters;

/// <summary>
/// Action filter that enforces idempotency on POST endpoints using a distributed cache (Redis).
/// Clients must send an <c>Idempotency-Key</c> header with a unique value (typically a GUID).
/// If the same key is sent again within the TTL window, the original response is replayed
/// without re-executing the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class IdempotencyFilter : Attribute, IAsyncActionFilter
{
    /// <summary>The HTTP header name used to carry the idempotency key.</summary>
    public const string HeaderName = "Idempotency-Key";

    private const int DefaultTtlMinutes = 60;
    private const int MaxKeyLength = 64;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(DefaultTtlMinutes)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            context.Result = new BadRequestObjectResult(new
            {
                message = $"The '{HeaderName}' header is required.",
                errorCode = Application.Constants.ErrorCodes.IdempotencyKeyMissing
            });
            return;
        }

        var idempotencyKey = keyValues.ToString().Trim();

        if (idempotencyKey.Length > MaxKeyLength)
        {
            context.Result = new BadRequestObjectResult(new
            {
                message = $"The '{HeaderName}' header must not exceed {MaxKeyLength} characters.",
                errorCode = Application.Constants.ErrorCodes.IdempotencyKeyTooLong
            });
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
        var cacheKey = $"idempotency:{idempotencyKey}";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            var cachedResponse = JsonSerializer.Deserialize<CachedResponse>(cached, JsonOptions)!;

            context.HttpContext.Response.StatusCode = cachedResponse.StatusCode;
            context.Result = new ContentResult
            {
                StatusCode = cachedResponse.StatusCode,
                Content = cachedResponse.Body,
                ContentType = "application/json"
            };
            return;
        }

        var executedContext = await next();

        if (executedContext.Exception is null && executedContext.Result is ObjectResult objectResult)
        {
            var responseBody = JsonSerializer.Serialize(objectResult.Value, JsonOptions);
            var entry = new CachedResponse(objectResult.StatusCode ?? 200, responseBody);
            var json = JsonSerializer.Serialize(entry, JsonOptions);

            await cache.SetStringAsync(cacheKey, json, CacheOptions);
        }
    }

    private sealed record CachedResponse(int StatusCode, string? Body);
}
