using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="ResendEmailService"/>.
/// </summary>
public sealed class ResendEmailServiceTests
{
    private static readonly ResendOptions DefaultOptions = new()
    {
        ApiKey = "test-api-key",
        FromEmail = "noreply@test.com"
    };

    private readonly IResend _resend = Substitute.For<IResend>();
    private readonly ILogger<ResendEmailService> _logger = Substitute.For<ILogger<ResendEmailService>>();

    private ResendEmailService CreateService() =>
        new(_resend, Options.Create(DefaultOptions), _logger);

    [Fact]
    public async Task SendPasswordResetEmailAsync_Success_DoesNotThrow()
    {
        var sut = CreateService();

        var exception = await Record.ExceptionAsync(() =>
            sut.SendPasswordResetEmailAsync(
                "user@example.com",
                "user@example.com",
                "https://app.com/reset?userId=1&token=abc"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ResendThrows_ThrowsServiceUnavailableException()
    {
        _resend
            .EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResendResponse<Guid>>(new Exception("Resend API error")));

        var sut = CreateService();

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            sut.SendPasswordResetEmailAsync(
                "user@example.com",
                "user@example.com",
                "https://app.com/reset?userId=1&token=abc"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ResendThrowsUnauthorized_ThrowsServiceUnavailableException()
    {
        _resend
            .EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResendResponse<Guid>>(new Exception("401 Unauthorized")));

        var sut = CreateService();

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            sut.SendPasswordResetEmailAsync(
                "user@example.com",
                "user@example.com",
                "https://app.com/reset?userId=1&token=abc"));
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ResendThrowsUnprocessable_ThrowsServiceUnavailableException()
    {
        _resend
            .EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResendResponse<Guid>>(new Exception("422 Unprocessable Entity")));

        var sut = CreateService();

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            sut.SendPasswordResetEmailAsync(
                "user@example.com",
                "user@example.com",
                "https://app.com/reset?userId=1&token=abc"));
    }
}
