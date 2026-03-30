namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for sending transactional emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a password reset email to the specified address.
    /// </summary>
    /// <param name="toEmail">The recipient email address.</param>
    /// <param name="userName">The display name shown in the email body.</param>
    /// <param name="resetUrl">The full password reset URL including userId and token query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="Application.Exceptions.ServiceUnavailableException">
    /// Thrown when the email provider is unavailable or returns an error response.
    /// </exception>
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetUrl,
        CancellationToken cancellationToken = default);
}
