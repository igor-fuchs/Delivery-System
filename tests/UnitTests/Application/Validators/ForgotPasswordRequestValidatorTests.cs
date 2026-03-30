using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

/// <summary>
/// Unit tests for <see cref="ForgotPasswordRequestValidator"/>.
/// </summary>
public sealed class ForgotPasswordRequestValidatorTests
{
    private readonly ForgotPasswordRequestValidator _sut = new();

    private static ForgotPasswordRequest ValidRequest => new(
        Email: "user@example.com",
        CaptchaToken: "valid-token",
        CallbackUrl: "https://app.example.com/reset-password");

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
        var result = await _sut.TestValidateAsync(ValidRequest with { Email = email! });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_InvalidEmailFormat_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { Email = "not-an-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_EmailTooLong_ShouldHaveError()
    {
        var longEmail = new string('a', 250) + "@x.co";
        var result = await _sut.TestValidateAsync(ValidRequest with { Email = longEmail });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyCaptchaToken_ShouldHaveError(string? token)
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { CaptchaToken = token! });
        result.ShouldHaveValidationErrorFor(x => x.CaptchaToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyCallbackUrl_ShouldHaveError(string? url)
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { CallbackUrl = url! });
        result.ShouldHaveValidationErrorFor(x => x.CallbackUrl);
    }

    [Fact]
    public async Task Validate_RelativeCallbackUrl_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { CallbackUrl = "/reset-password" });
        result.ShouldHaveValidationErrorFor(x => x.CallbackUrl);
    }

    [Fact]
    public async Task Validate_NonHttpCallbackUrl_ShouldHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { CallbackUrl = "ftp://example.com/reset" });
        result.ShouldHaveValidationErrorFor(x => x.CallbackUrl);
    }

    [Fact]
    public async Task Validate_HttpCallbackUrl_ShouldNotHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { CallbackUrl = "http://localhost:3000/reset" });
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUrl);
    }

    [Fact]
    public async Task Validate_HttpsCallbackUrl_ShouldNotHaveError()
    {
        var result = await _sut.TestValidateAsync(ValidRequest with { CallbackUrl = "https://app.example.com/reset-password" });
        result.ShouldNotHaveValidationErrorFor(x => x.CallbackUrl);
    }
}
