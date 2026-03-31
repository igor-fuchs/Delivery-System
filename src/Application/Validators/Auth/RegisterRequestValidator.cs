using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="RegisterRequest"/>.
/// Enforces name format, email format, and password complexity requirements.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.").WithErrorCode(ErrorCodes.ValidationFailed)
            .EmailAddress().WithMessage("Invalid email format.").WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.").WithErrorCode(ErrorCodes.ValidationFailed);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.").WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.").WithErrorCode(ErrorCodes.ValidationFailed)
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.").WithErrorCode(ErrorCodes.ValidationFailed)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.").WithErrorCode(ErrorCodes.ValidationFailed)
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.").WithErrorCode(ErrorCodes.ValidationFailed)
            .Matches(@"\d").WithMessage("Password must contain at least one number.").WithErrorCode(ErrorCodes.ValidationFailed)
            .Matches(@"[@$!%*?&]").WithMessage("Password must contain at least one special character.").WithErrorCode(ErrorCodes.ValidationFailed);

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("Captcha token is required.").WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(2000).WithMessage("Captcha token cannot exceed 2000 characters.").WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
