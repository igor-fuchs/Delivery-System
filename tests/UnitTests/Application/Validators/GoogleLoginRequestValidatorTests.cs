using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Validators;
using FluentValidation.TestHelper;

namespace DeliverySystem.UnitTests.Application.Validators;

public sealed class GoogleLoginRequestValidatorTests
{
    private readonly GoogleLoginRequestValidator _sut = new();

    [Fact]
    public async Task Validate_ValidIdToken_ShouldNotHaveErrors()
    {
        var request = new GoogleLoginRequest("valid-google-id-token");

        var result = await _sut.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyIdToken_ShouldHaveError(string? idToken)
    {
        var request = new GoogleLoginRequest(idToken!);

        var result = await _sut.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Token)
              .WithErrorMessage("Google ID token is required.");
    }
}
