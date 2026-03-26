using DeliverySystem.Application.Constants;
using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.UnitTests.Application.Exceptions;

public sealed class ConflictExceptionTests
{
    [Fact]
    public void Constructor_ShouldStoreMessageAndCode()
    {
        var exception = new ConflictException("User already exists.", ErrorCodes.UserAlreadyExists);

        Assert.Equal("User already exists.", exception.Message);
        Assert.Equal(ErrorCodes.UserAlreadyExists, exception.Code);
    }
}
