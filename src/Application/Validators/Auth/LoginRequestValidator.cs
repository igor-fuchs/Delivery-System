using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="LoginRequest"/>.
/// Ensures email and password are provided.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.").WithErrorCode(ErrorCodes.EmailRequired)
            .EmailAddress().WithMessage("Invalid email format.").WithErrorCode(ErrorCodes.EmailInvalidFormat);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.").WithErrorCode(ErrorCodes.PasswordRequired);

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("Captcha token is required.").WithErrorCode(ErrorCodes.CaptchaTokenRequired);
    }
}
