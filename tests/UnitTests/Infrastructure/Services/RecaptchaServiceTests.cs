using System.Net;
using System.Text.Json;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

public sealed class RecaptchaServiceTests
{
    private static readonly RecaptchaOptions DefaultOptions = new()
    {
        SecretKey = "test-secret-key",
        MinimumScore = 0.5
    };

    private readonly ILogger<RecaptchaService> _logger = Substitute.For<ILogger<RecaptchaService>>();

    /// <summary>
    /// Creates a <see cref="RecaptchaService"/> backed by a fake HTTP handler
    /// that returns the specified JSON body and status code.
    /// </summary>
    private RecaptchaService CreateService(object responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(
            JsonSerializer.Serialize(responseBody),
            statusCode);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.google.com/recaptcha/api/")
        };

        return new RecaptchaService(httpClient, Options.Create(DefaultOptions), _logger);
    }

    [Fact]
    public async Task ValidateAsync_SuccessWithHighScore_ShouldReturnTrue()
    {
        var sut = CreateService(new { success = true, score = 0.9 });

        var result = await sut.ValidateAsync("valid-token");

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_SuccessWithNoScore_ShouldReturnTrue()
    {
        // reCAPTCHA v2 does not return a score field.
        var sut = CreateService(new { success = true });

        var result = await sut.ValidateAsync("valid-token");

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_SuccessWithLowScore_ShouldReturnFalse()
    {
        var sut = CreateService(new { success = true, score = 0.2 });

        var result = await sut.ValidateAsync("low-score-token");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_FailedVerification_ShouldReturnFalse()
    {
        var sut = CreateService(new
        {
            success = false,
            error_codes = new[] { "invalid-input-response" }
        });

        var result = await sut.ValidateAsync("bad-token");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_HttpError_ShouldReturnFalse()
    {
        var sut = CreateService(
            new { success = false },
            HttpStatusCode.InternalServerError);

        var result = await sut.ValidateAsync("any-token");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_NullBody_ShouldReturnFalse()
    {
        // "null" is valid JSON but deserializes to null.
        var handler = new FakeHttpMessageHandler("null", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.google.com/recaptcha/api/")
        };
        var sut = new RecaptchaService(httpClient, Options.Create(DefaultOptions), _logger);

        var result = await sut.ValidateAsync("any-token");

        Assert.False(result);
    }

    /// <summary>
    /// A minimal <see cref="HttpMessageHandler"/> that returns a preconfigured response.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
