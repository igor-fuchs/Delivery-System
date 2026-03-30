using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

/// <summary>
/// Unit tests for <see cref="ResetPasswordRequestValidator"/>.
/// </summary>
public sealed class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _sut = new();

    private static ResetPasswordRequest ValidRequest => new(
        UserId: Guid.NewGuid().ToString(),
        Token: "valid-reset-token",
        NewPassword: "NewP@ssw0rd!",
        CaptchaToken: "valid-captcha");

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveErrors()
    {
        var result = await _sut.TestValidateAsync(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyUserId_ShouldHaveError(string? userId)
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { UserId = userId! });
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyToken_ShouldHaveError(string? token)
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { Token = token! });
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyNewPassword_ShouldHaveError(string? password)
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { NewPassword = password! });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public async Task Validate_PasswordTooShort_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { NewPassword = "Ab1!" });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public async Task Validate_PasswordMissingUppercase_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { NewPassword = "newp@ssw0rd!" });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public async Task Validate_PasswordMissingLowercase_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { NewPassword = "NEWP@SSW0RD!" });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public async Task Validate_PasswordMissingDigit_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { NewPassword = "NewP@ssword!" });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public async Task Validate_PasswordMissingSpecialChar_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { NewPassword = "NewPassw0rd" });
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyCaptchaToken_ShouldHaveError(string? token)
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { CaptchaToken = token! });
        result.ShouldHaveValidationErrorFor(x => x.CaptchaToken);
    }
}
