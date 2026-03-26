using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="CreateOrderRequest"/>.
/// Validates structure and checks that referenced products exist in the database.
/// </summary>
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrderRequestValidator"/> class.
    /// </summary>
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.").WithErrorCode(ErrorCodes.OrderDescriptionRequired)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.").WithErrorCode(ErrorCodes.OrderDescriptionTooLong);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.").WithErrorCode(ErrorCodes.OrderItemsRequired);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product ID is required.").WithErrorCode(ErrorCodes.OrderItemProductIdRequired);

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.").WithErrorCode(ErrorCodes.OrderItemQuantityTooLow)
                .LessThan(1000).WithMessage("Quantity must be less than 1000.").WithErrorCode(ErrorCodes.OrderItemQuantityTooHigh);
        });
    }
}
