using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
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
    /// <param name="productService">The product service used to verify product existence.</param>
    public CreateOrderRequestValidator(IProductService productService)
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product ID is required.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
                .LessThan(1000).WithMessage("Quantity must be less than 1000.");

            item.RuleFor(i => i.ProductId)
                .MustAsync(async (id, ct) => id != Guid.Empty && await productService.ExistsAsync(id, ct))
                .WithMessage("The specified product was not found.")
                .When(i => i.ProductId != Guid.Empty);
        });
    }
}
