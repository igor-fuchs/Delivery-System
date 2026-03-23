using DeliverySystem.Application.Interfaces;

namespace DeliverySystem.IntegrationTests.Infrastructure;

/// <summary>
/// Fake CAPTCHA service used in integration tests.
/// Returns a configurable result (defaults to <c>true</c>).
/// </summary>
public sealed class FakeCaptchaService : ICaptchaService
{
    /// <summary>Gets or sets whether the next validation should succeed.</summary>
    public bool ShouldPass { get; set; } = true;

    /// <inheritdoc />
    public Task<bool> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ShouldPass);
    }
}
