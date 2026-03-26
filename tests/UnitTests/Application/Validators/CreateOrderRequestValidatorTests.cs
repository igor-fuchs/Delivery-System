using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

public sealed class CreateOrderRequestValidatorTests
{
    private static readonly Guid ExistingProductId = Guid.NewGuid();
    private readonly CreateOrderRequestValidator _sut;

    public CreateOrderRequestValidatorTests()
    {
        _sut = new CreateOrderRequestValidator();

    }

    private static CreateOrderRequest ValidRequest => new(
        Description: "Standard delivery order.",
        Items: [new CreateOrderItemRequest(ExistingProductId, 2)]);

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveErrors()
    {
        var result = await _sut.TestValidateAsync(ValidRequest);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #region Description

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyDescription_ShouldHaveError(string? description)
    {
        var request = ValidRequest with { Description = description! };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public async Task Validate_DescriptionExceeds2000Chars_ShouldHaveError()
    {
        var request = ValidRequest with { Description = new string('a', 2001) };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 2000 characters.");
    }

    #endregion

    #region Items

    [Fact]
    public async Task Validate_EmptyItems_ShouldHaveError()
    {
        var request = ValidRequest with { Items = [] };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Items)
              .WithErrorMessage("Order must contain at least one item.");
    }

    [Fact]
    public async Task Validate_ItemWithEmptyProductId_ShouldHaveError()
    {
        var request = ValidRequest with { Items = [new CreateOrderItemRequest(Guid.Empty, 1)] };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
              .WithErrorMessage("Product ID is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_ItemWithZeroOrNegativeQuantity_ShouldHaveError(int quantity)
    {
        var request = ValidRequest with { Items = [new CreateOrderItemRequest(ExistingProductId, quantity)] };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
              .WithErrorMessage("Quantity must be greater than zero.");
    }

    [Fact]
    public async Task Validate_ItemWithQuantityExceedsMaximum_ShouldHaveError()
    {
        var request = ValidRequest with { Items = [new CreateOrderItemRequest(ExistingProductId, 1000)] };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
              .WithErrorMessage("Quantity must be less than 1000.");
    }

    #endregion
}
