using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    private static RegisterRequest ValidRequest => new(
        Email: "user@example.com",
        Password: "Str0ng!Pass",
        CaptchaToken: "valid-token");

    #region Email

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveErrors()
    {
        var result = await _sut.TestValidateAsync(ValidRequest);

        result.ShouldNotHaveAnyValidationErrors();
    }

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

    [Fact]
    public async Task Validate_EmailExceeds254Chars_ShouldHaveError()
    {
        var longEmail = new string('a', 243) + "@example.com"; // 255 chars

        var request = ValidRequest with { Email = longEmail };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email cannot exceed 254 characters.");
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

    [Fact]
    public async Task Validate_PasswordTooShort_ShouldHaveError()
    {
        var request = ValidRequest with { Password = "Aa1!xyz" }; // 7 chars

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Fact]
    public async Task Validate_PasswordExceeds128Chars_ShouldHaveError()
    {
        var longPassword = "Aa1!" + new string('x', 125); // 129 chars

        var request = ValidRequest with { Password = longPassword };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password cannot exceed 128 characters.");
    }

    [Fact]
    public async Task Validate_PasswordMissingUppercase_ShouldHaveError()
    {
        var request = ValidRequest with { Password = "str0ng!pass" };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public async Task Validate_PasswordMissingLowercase_ShouldHaveError()
    {
        var request = ValidRequest with { Password = "STR0NG!PASS" };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public async Task Validate_PasswordMissingDigit_ShouldHaveError()
    {
        var request = ValidRequest with { Password = "Strong!Pass" };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one number.");
    }

    [Fact]
    public async Task Validate_PasswordMissingSpecialChar_ShouldHaveError()
    {
        var request = ValidRequest with { Password = "Str0ngPass1" };

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one special character.");
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
