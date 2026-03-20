using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.UnitTests.Application.Exceptions;

public sealed class ValidationExceptionTests
{
    [Fact]
    public void Constructor_ShouldStoreErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required."],
            ["Password"] = ["Too short.", "Missing digit."]
        };

        var exception = new ValidationException(errors);

        Assert.Equal("One or more validation errors occurred.", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal(["Email is required."], exception.Errors["Email"]);
        Assert.Equal(["Too short.", "Missing digit."], exception.Errors["Password"]);
    }

    [Fact]
    public void Constructor_EmptyErrors_ShouldStoreEmptyDictionary()
    {
        var errors = new Dictionary<string, string[]>();

        var exception = new ValidationException(errors);

        Assert.Empty(exception.Errors);
    }
}
