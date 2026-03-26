using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

public sealed class UpdateOrderStatusRequestValidatorTests
{
    private readonly UpdateOrderStatusRequestValidator _sut = new();

    [Theory]
    [InlineData("Pending")]
    [InlineData("Processing")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    [InlineData("pending")]   // case-insensitive
    [InlineData("SHIPPED")]
    public async Task Validate_ValidStatus_ShouldNotHaveErrors(string status)
    {
        var result = await _sut.TestValidateAsync(new UpdateOrderStatusRequest(status));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyStatus_ShouldHaveError(string? status)
    {
        var result = await _sut.TestValidateAsync(new UpdateOrderStatusRequest(status!));

        result.ShouldHaveValidationErrorFor(x => x.Status)
              .WithErrorMessage("Status is required.");
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("InProgress")]
    [InlineData("Done")]
    public async Task Validate_InvalidStatus_ShouldHaveError(string status)
    {
        var result = await _sut.TestValidateAsync(new UpdateOrderStatusRequest(status));

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
