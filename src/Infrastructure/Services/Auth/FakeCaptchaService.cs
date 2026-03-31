using DeliverySystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Fake CAPTCHA service that always returns success.
/// Used in the Development environment to bypass real reCAPTCHA validation.
/// </summary>
public sealed class FakeCaptchaService : ICaptchaService
{
    private readonly ILogger<FakeCaptchaService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeCaptchaService"/> class.
    /// </summary>
    /// <param name="logger">Logger for recording bypass events.</param>
    public FakeCaptchaService(ILogger<FakeCaptchaService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>Always returns <c>true</c> in development mode.</remarks>
    public Task<bool> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("CAPTCHA validation bypassed (Development environment)");
        return Task.FromResult(true);
    }
}
