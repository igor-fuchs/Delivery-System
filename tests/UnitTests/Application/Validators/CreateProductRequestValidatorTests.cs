using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

public sealed class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _sut = new();

    private static CreateProductRequest ValidRequest => new(
        Name: "Widget",
        Description: "A useful widget.",
        Stock: true,
        Price: 9.99m);

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveErrors()
    {
        var result = await _sut.TestValidateAsync(ValidRequest);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #region Name

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ShouldHaveError(string? name)
    {
        var request = ValidRequest with { Name = name! };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public async Task Validate_NameExceeds200Chars_ShouldHaveError()
    {
        var request = ValidRequest with { Name = new string('a', 201) };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name cannot exceed 200 characters.");
    }

    #endregion

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

    #region Price

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_PriceNotPositive_ShouldHaveError(decimal price)
    {
        var request = ValidRequest with { Price = price };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must be greater than zero.");
    }

    [Fact]
    public async Task Validate_PriceExceedsMaximum_ShouldHaveError()
    {
        var request = ValidRequest with { Price = 10000000m };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must be less than 10000000.");
    }

    #endregion
}
