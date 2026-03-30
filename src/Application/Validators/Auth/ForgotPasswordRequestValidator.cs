using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="ForgotPasswordRequest"/>.
/// Validates email format, captcha token, and that the callback URL is a valid absolute HTTP/HTTPS URI.
/// </summary>
public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.").WithErrorCode(ErrorCodes.EmailRequired)
            .EmailAddress().WithMessage("Invalid email format.").WithErrorCode(ErrorCodes.EmailInvalidFormat)
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.").WithErrorCode(ErrorCodes.EmailTooLong);

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("Captcha token is required.").WithErrorCode(ErrorCodes.CaptchaTokenRequired)
            .MaximumLength(2000).WithMessage("Captcha token cannot exceed 2000 characters.").WithErrorCode(ErrorCodes.CaptchaTokenTooLong);

        RuleFor(x => x.CallbackUrl)
            .NotEmpty().WithMessage("Callback URL is required.").WithErrorCode(ErrorCodes.CallbackUrlRequired)
            .MaximumLength(2000).WithMessage("Callback URL cannot exceed 2000 characters.").WithErrorCode(ErrorCodes.CallbackUrlTooLong)
            .Must(BeAbsoluteHttpOrHttpsUri).WithMessage("Callback URL must be a valid absolute HTTP or HTTPS URL.").WithErrorCode(ErrorCodes.CallbackUrlInvalidFormat);
    }

    private static bool BeAbsoluteHttpOrHttpsUri(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
