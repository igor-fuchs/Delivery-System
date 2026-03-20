using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    private static LoginRequest ValidRequest => new(
        Email: "user@example.com",
        Password: "any-password",
        CaptchaToken: "valid-token");

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveErrors()
    {
        var result = await _sut.TestValidateAsync(ValidRequest);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #region Email

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyEmail_ShouldHaveError(string? email)
    {
        var request = ValidRequest with { Email = email! };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required.");
    }

    [Fact]
    public async Task Validate_InvalidEmailFormat_ShouldHaveError()
    {
        var request = ValidRequest with { Email = "not-an-email" };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Invalid email format.");
    }

    #endregion

    #region Password

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyPassword_ShouldHaveError(string? password)
    {
        var request = ValidRequest with { Password = password! };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is required.");
    }

    #endregion

    #region CaptchaToken

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyCaptchaToken_ShouldHaveError(string? token)
    {
        var request = ValidRequest with { CaptchaToken = token! };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.CaptchaToken)
              .WithErrorMessage("Captcha token is required.");
    }

    #endregion
}
