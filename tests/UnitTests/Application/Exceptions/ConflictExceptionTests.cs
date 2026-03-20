using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.UnitTests.Application.Exceptions;

public sealed class ConflictExceptionTests
{
    [Fact]
    public void Constructor_ShouldStoreMessage()
    {
        var exception = new ConflictException("User already exists.");

        Assert.Equal("User already exists.", exception.Message);
    }
}
