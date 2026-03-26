using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Domain.Enums;
using FluentValidation;

namespace DeliverySystem.Application.Validators;

/// <summary>
/// FluentValidation validator for <see cref="UpdateOrderStatusRequest"/>.
/// </summary>
public sealed class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.").WithErrorCode(ErrorCodes.OrderStatusRequired)
            .Must(s => Enum.TryParse<OrderStatus>(s, ignoreCase: true, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<OrderStatus>())}.")
            .WithErrorCode(ErrorCodes.OrderStatusInvalid);
    }
}
