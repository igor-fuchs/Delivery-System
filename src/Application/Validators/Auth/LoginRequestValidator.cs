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
            .NotEmpty().WithMessage("Email is required.").WithErrorCode(ErrorCodes.ValidationFailed)
            .EmailAddress().WithMessage("Invalid email format.").WithErrorCode(ErrorCodes.ValidationFailed);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.").WithErrorCode(ErrorCodes.ValidationFailed);

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("Captcha token is required.").WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
