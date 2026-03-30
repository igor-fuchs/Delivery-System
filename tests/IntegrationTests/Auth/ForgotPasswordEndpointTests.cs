using System.Net;
using System.Net.Http.Json;
using System.Web;
using DeliverySystem.IntegrationTests.Infrastructure;

namespace DeliverySystem.IntegrationTests.Auth;

/// <summary>
/// End-to-end tests for the <c>POST /api/auth/forgot-password</c> endpoint.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ForgotPasswordEndpointTests : IntegrationTestBase
{
    public ForgotPasswordEndpointTests(DeliverySystemFactory factory) : base(factory) { }

    private Task<HttpResponseMessage> ForgotPasswordAsync(string email, string callbackUrl = "https://app.example.com/reset-password")
    {
        return Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = email,
            CaptchaToken = "valid-captcha-token",
            CallbackUrl = callbackUrl
        });
    }

    [Fact]
    public async Task ForgotPassword_RegisteredEmail_ReturnsOk()
    {
        var email = $"fp-ok-{Guid.NewGuid()}@test.com";
        await RegisterAsync(email, "P@ssw0rd!");

        var response = await ForgotPasswordAsync(email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsOkWithoutRevealingExistence()
    {
        var response = await ForgotPasswordAsync($"nobody-{Guid.NewGuid()}@test.com");

        // Must return 200 to prevent user enumeration.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_RegisteredEmail_CapturesEmailWithResetLink()
    {
        var email = $"fp-capture-{Guid.NewGuid()}@test.com";
        await RegisterAsync(email, "P@ssw0rd!");

        await ForgotPasswordAsync(email, "https://app.example.com/reset-password");

        Assert.Single(Factory.EmailService.SentEmails);
        var sent = Factory.EmailService.SentEmails[0];
        Assert.Equal(email, sent.ToEmail);
        Assert.Contains("userId=", sent.ResetUrl);
        Assert.Contains("token=", sent.ResetUrl);
        Assert.StartsWith("https://app.example.com/reset-password", sent.ResetUrl);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNotSendEmail()
    {
        await ForgotPasswordAsync($"unknown-{Guid.NewGuid()}@test.com");

        Assert.Empty(Factory.EmailService.SentEmails);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmailFormat_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = "not-an-email",
            CaptchaToken = "valid-captcha-token",
            CallbackUrl = "https://app.example.com/reset-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_MissingEmail_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = "",
            CaptchaToken = "valid-captcha-token",
            CallbackUrl = "https://app.example.com/reset-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_MissingCaptchaToken_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = "user@example.com",
            CaptchaToken = "",
            CallbackUrl = "https://app.example.com/reset-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_RelativeCallbackUrl_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = "user@example.com",
            CaptchaToken = "valid-captcha-token",
            CallbackUrl = "/reset-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_CaptchaFails_ReturnsUnauthorized()
    {
        Factory.CaptchaService.ShouldPass = false;

        var response = await ForgotPasswordAsync("user@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_TokenInResetUrl_IsUrlEncoded()
    {
        var email = $"fp-urlenc-{Guid.NewGuid()}@test.com";
        await RegisterAsync(email, "P@ssw0rd!");

        await ForgotPasswordAsync(email);

        Assert.Single(Factory.EmailService.SentEmails);
        var resetUrl = Factory.EmailService.SentEmails[0].ResetUrl;
        var uri = new Uri(resetUrl);
        var query = HttpUtility.ParseQueryString(uri.Query);

        // userId and token must be present as query params
        Assert.NotNull(query["userId"]);
        Assert.NotNull(query["token"]);
        Assert.NotEmpty(query["userId"]!);
        Assert.NotEmpty(query["token"]!);
    }
}
