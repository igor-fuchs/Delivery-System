namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for CAPTCHA token verification.
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Validates a CAPTCHA token obtained from the client.
    /// </summary>
    /// <param name="token">The CAPTCHA token to verify.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the token is valid; otherwise <c>false</c>.</returns>
    Task<bool> ValidateAsync(string token, CancellationToken cancellationToken = default);
}
