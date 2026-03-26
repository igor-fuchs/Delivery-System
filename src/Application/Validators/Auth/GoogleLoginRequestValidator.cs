using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="GoogleLoginRequest"/>.
/// Ensures the Google ID token is provided.
/// </summary>
public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Google ID token is required.").WithErrorCode(ErrorCodes.GoogleTokenRequired);
    }
}
