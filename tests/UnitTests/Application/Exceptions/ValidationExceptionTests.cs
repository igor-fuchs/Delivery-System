using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.UnitTests.Application.Exceptions;

public sealed class ValidationExceptionTests
{
    [Fact]
    public void Constructor_ShouldStoreErrors()
    {
        var errors = new Dictionary<string, ValidationFieldError[]>
        {
            ["Email"] = [new ValidationFieldError("EMAIL_REQUIRED", "Email is required.")],
            ["Password"] = [new ValidationFieldError("PASSWORD_TOO_SHORT", "Too short."), new ValidationFieldError("PASSWORD_MISSING_DIGIT", "Missing digit.")]
        };

        var exception = new ValidationException(errors);

        Assert.Equal("Validation failed.", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal("EMAIL_REQUIRED", exception.Errors["Email"][0].Code);
        Assert.Equal("Email is required.", exception.Errors["Email"][0].Message);
        Assert.Equal(2, exception.Errors["Password"].Length);
    }

    [Fact]
    public void Constructor_EmptyErrors_ShouldStoreEmptyDictionary()
    {
        var errors = new Dictionary<string, ValidationFieldError[]>();

        var exception = new ValidationException(errors);

        Assert.Empty(exception.Errors);
    }
}
