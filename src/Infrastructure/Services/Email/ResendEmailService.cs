using DeliverySystem.Application.Constants;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Sends transactional emails via the Resend .NET SDK (https://resend.com).
/// </summary>
public sealed class ResendEmailService : IEmailService
{
  private readonly IResend _resend;
  private readonly ResendOptions _options;
  private readonly ILogger<ResendEmailService> _logger;

  /// <summary>
  /// Initializes a new instance of the <see cref="ResendEmailService"/> class.
  /// </summary>
  /// <param name="resend">The Resend client used to send emails.</param>
  /// <param name="options">Resend API configuration options.</param>
  /// <param name="logger">Logger for email send events and errors.</param>
  public ResendEmailService(
      IResend resend,
      IOptions<ResendOptions> options,
      ILogger<ResendEmailService> logger)
  {
    _resend = resend;
    _options = options.Value;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task SendPasswordResetEmailAsync(
      string toEmail,
      string userName,
      string resetUrl,
      CancellationToken cancellationToken = default)
  {
    var message = new EmailMessage
    {
      From = _options.FromEmail,
      Subject = "Reset your password",
      HtmlBody = BuildHtmlTemplate(userName, resetUrl)
    };
    message.To.Add(toEmail);

    try
    {
      await _resend.EmailSendAsync(message, cancellationToken);
      _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
    catch (Exception ex)
    {
      _logger.LogError(
          ex,
          "Resend failed to deliver password reset email to {Email}",
          toEmail);

      throw new ServiceUnavailableException(
          "Email delivery failed. Please try again later.",
          ErrorCodes.EmailDeliveryFailed);
    }
  }

  /// <summary>
  /// Builds the personalized HTML email body for the password reset message.
  /// </summary>
  /// <param name="userName">The recipient's display name or email address.</param>
  /// <param name="resetUrl">The full password reset URL.</param>
  /// <returns>An HTML string with a styled reset button and expiry notice.</returns>
  private static string BuildHtmlTemplate(string userName, string resetUrl) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Reset your password</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f5;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.06);">

                  <!-- Header -->
                  <tr>
                    <td style="background-color:#18181b;padding:32px 40px;">
                      <h1 style="margin:0;color:#ffffff;font-size:22px;font-weight:700;letter-spacing:-0.3px;">
                        DeliverySystem
                      </h1>
                    </td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:40px 40px 32px;">
                      <h2 style="margin:0 0 16px;color:#18181b;font-size:20px;font-weight:600;">
                        Reset your password
                      </h2>
                      <p style="margin:0 0 16px;color:#3f3f46;font-size:15px;line-height:1.6;">
                        Hi <strong>{System.Net.WebUtility.HtmlEncode(userName)}</strong>,
                      </p>
                      <p style="margin:0 0 32px;color:#3f3f46;font-size:15px;line-height:1.6;">
                        We received a request to reset the password for your account.
                        Click the button below to choose a new password. This link will expire in
                        <strong>24 hours</strong>.
                      </p>

                      <!-- CTA Button -->
                      <table cellpadding="0" cellspacing="0" style="margin:0 0 32px;">
                        <tr>
                          <td style="border-radius:6px;background-color:#18181b;">
                            <a href="{System.Net.WebUtility.HtmlEncode(resetUrl)}"
                               target="_blank"
                               style="display:inline-block;padding:14px 28px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;">
                              Reset password
                            </a>
                          </td>
                        </tr>
                      </table>

                      <hr style="border:none;border-top:1px solid #e4e4e7;margin:0 0 24px;" />

                      <p style="margin:0;color:#71717a;font-size:13px;line-height:1.6;">
                        If you did not request a password reset, you can safely ignore this email.
                        Your password will not be changed.
                      </p>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background-color:#f4f4f5;padding:24px 40px;border-top:1px solid #e4e4e7;">
                      <p style="margin:0;color:#a1a1aa;font-size:12px;text-align:center;">
                        &copy; {DateTime.UtcNow.Year} DeliverySystem. All rights reserved.
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
