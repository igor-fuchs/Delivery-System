using System.Net;
using System.Net.Http.Json;
using System.Web;
using DeliverySystem.IntegrationTests.Infrastructure;

namespace DeliverySystem.IntegrationTests.Auth;

/// <summary>
/// End-to-end tests for the <c>POST /api/auth/reset-password</c> endpoint.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ResetPasswordEndpointTests : IntegrationTestBase
{
    public ResetPasswordEndpointTests(DeliverySystemFactory factory) : base(factory) { }

    private Task<HttpResponseMessage> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        return Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            UserId = userId,
            Token = token,
            NewPassword = newPassword,
            CaptchaToken = "valid-captcha-token"
        });
    }

    /// <summary>
    /// Registers a user, triggers forgot-password, and extracts the userId and token from the captured email.
    /// </summary>
    private async Task<(string UserId, string Token)> GetResetCredentialsAsync(string email, string password)
    {
        await RegisterAsync(email, password);

        await Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = email,
            CaptchaToken = "valid-captcha-token",
            CallbackUrl = "https://app.example.com/reset-password"
        });

        var resetUrl = Factory.EmailService.SentEmails.Last().ResetUrl;
        var uri = new Uri(resetUrl);
        var query = HttpUtility.ParseQueryString(uri.Query);

        return (query["userId"]!, query["token"]!);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOk()
    {
        var email = $"rp-ok-{Guid.NewGuid()}@test.com";
        var (userId, token) = await GetResetCredentialsAsync(email, "P@ssw0rd!");

        var response = await ResetPasswordAsync(userId, token, "NewP@ssw0rd!");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_AllowsLoginWithNewPassword()
    {
        var email = $"rp-newpw-{Guid.NewGuid()}@test.com";
        var (userId, token) = await GetResetCredentialsAsync(email, "P@ssw0rd!");

        await ResetPasswordAsync(userId, token, "NewP@ssw0rd!");

        var loginResponse = await LoginAsync(email, "NewP@ssw0rd!");
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_BlocksLoginWithOldPassword()
    {
        var email = $"rp-oldpw-{Guid.NewGuid()}@test.com";
        var (userId, token) = await GetResetCredentialsAsync(email, "P@ssw0rd!");

        await ResetPasswordAsync(userId, token, "NewP@ssw0rd!");

        var loginResponse = await LoginAsync(email, "P@ssw0rd!");
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsUnauthorized()
    {
        var email = $"rp-badtok-{Guid.NewGuid()}@test.com";
        await RegisterAsync(email, "P@ssw0rd!");

        // Trigger forgot-password to get a real userId, then use a garbage token.
        await Client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            Email = email,
            CaptchaToken = "valid-captcha-token",
            CallbackUrl = "https://app.example.com/reset-password"
        });

        var resetUrl = Factory.EmailService.SentEmails.Last().ResetUrl;
        var userId = HttpUtility.ParseQueryString(new Uri(resetUrl).Query)["userId"]!;

        var response = await ResetPasswordAsync(userId, "completely-invalid-token", "NewP@ssw0rd!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnknownUserId_ReturnsUnauthorized()
    {
        var response = await ResetPasswordAsync(Guid.NewGuid().ToString(), "some-token", "NewP@ssw0rd!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_TokenUsedTwice_SecondAttemptReturnsUnauthorized()
    {
        var email = $"rp-reuse-{Guid.NewGuid()}@test.com";
        var (userId, token) = await GetResetCredentialsAsync(email, "P@ssw0rd!");

        await ResetPasswordAsync(userId, token, "NewP@ssw0rd1!");

        // Identity invalidates the token after first use.
        var response = await ResetPasswordAsync(userId, token, "AnotherP@ss2!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_EmptyUserId_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            UserId = "",
            Token = "some-token",
            NewPassword = "NewP@ssw0rd!",
            CaptchaToken = "valid-captcha-token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_EmptyToken_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            UserId = Guid.NewGuid().ToString(),
            Token = "",
            NewPassword = "NewP@ssw0rd!",
            CaptchaToken = "valid-captcha-token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WeakNewPassword_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            UserId = Guid.NewGuid().ToString(),
            Token = "some-token",
            NewPassword = "weak",
            CaptchaToken = "valid-captcha-token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_MissingCaptcha_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            UserId = Guid.NewGuid().ToString(),
            Token = "some-token",
            NewPassword = "NewP@ssw0rd!",
            CaptchaToken = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_CaptchaFails_ReturnsUnauthorized()
    {
        Factory.CaptchaService.ShouldPass = false;

        var response = await ResetPasswordAsync(Guid.NewGuid().ToString(), "some-token", "NewP@ssw0rd!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
