using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="ResetPasswordRequest"/>.
/// Validates the user ID, reset token, new password complexity, and captcha token.
/// </summary>
public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.").WithErrorCode(ErrorCodes.UserIdRequired)
            .MaximumLength(128).WithMessage("User ID cannot exceed 128 characters.").WithErrorCode(ErrorCodes.UserIdTooLong);

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.").WithErrorCode(ErrorCodes.ResetTokenRequired)
            .MaximumLength(2000).WithMessage("Reset token cannot exceed 2000 characters.").WithErrorCode(ErrorCodes.ResetTokenTooLong);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.").WithErrorCode(ErrorCodes.NewPasswordRequired)
            .MaximumLength(128).WithMessage("New password cannot exceed 128 characters.").WithErrorCode(ErrorCodes.NewPasswordTooLong)
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.").WithErrorCode(ErrorCodes.NewPasswordTooShort)
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.").WithErrorCode(ErrorCodes.NewPasswordMissingUppercase)
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.").WithErrorCode(ErrorCodes.NewPasswordMissingLowercase)
            .Matches(@"\d").WithMessage("New password must contain at least one number.").WithErrorCode(ErrorCodes.NewPasswordMissingDigit)
            .Matches(@"[@$!%*?&]").WithMessage("New password must contain at least one special character.").WithErrorCode(ErrorCodes.NewPasswordMissingSpecial);

        RuleFor(x => x.CaptchaToken)
            .NotEmpty().WithMessage("Captcha token is required.").WithErrorCode(ErrorCodes.CaptchaTokenRequired)
            .MaximumLength(2000).WithMessage("Captcha token cannot exceed 2000 characters.").WithErrorCode(ErrorCodes.CaptchaTokenTooLong);
    }
}
