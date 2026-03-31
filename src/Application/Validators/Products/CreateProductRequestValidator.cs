using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="CreateProductRequest"/>.
/// </summary>
public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.").WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.").WithErrorCode(ErrorCodes.ValidationFailed);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.").WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.").WithErrorCode(ErrorCodes.ValidationFailed);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.").WithErrorCode(ErrorCodes.ValidationFailed)
            .LessThan(10000000).WithMessage("Price must be less than 10000000.").WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
