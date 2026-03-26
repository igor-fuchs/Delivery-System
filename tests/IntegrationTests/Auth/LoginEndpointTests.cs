using System.Net;
using System.Net.Http.Json;
using DeliverySystem.IntegrationTests.Infrastructure;

namespace DeliverySystem.IntegrationTests.Auth;

/// <summary>
/// End-to-end tests for the <c>POST /api/auth/login</c> endpoint.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class LoginEndpointTests : IntegrationTestBase
{
    public LoginEndpointTests(DeliverySystemFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithAuthResponse()
    {
        var email = $"login-ok-{Guid.NewGuid()}@test.com";
        const string password = "P@ssw0rd!";

        await RegisterAsync(email, password);

        var response = await LoginAsync(email, password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(email, body!.Email);
        Assert.NotNull(body.Token);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsNotFound()
    {
        var response = await LoginAsync($"unknown-{Guid.NewGuid()}@test.com", "P@ssw0rd!");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = $"wrongpw-{Guid.NewGuid()}@test.com";
        await RegisterAsync(email, "P@ssw0rd!");

        var response = await LoginAsync(email, "WrongPassword1!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyEmail_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = "P@ssw0rd!",
            CaptchaToken = "valid"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "user@test.com",
            Password = "",
            CaptchaToken = "valid"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingCaptcha_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "user@test.com",
            Password = "P@ssw0rd!",
            CaptchaToken = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CaptchaFails_ReturnsUnauthorized()
    {
        var email = $"captcha-login-{Guid.NewGuid()}@test.com";
        await RegisterAsync(email, "P@ssw0rd!");

        Factory.CaptchaService.ShouldPass = false;

        var response = await LoginAsync(email, "P@ssw0rd!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsValidJwtToken()
    {
        var email = $"jwt-login-{Guid.NewGuid()}@test.com";
        const string password = "P@ssw0rd!";
        await RegisterAsync(email, password);

        var response = await LoginAsync(email, password);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body?.Token);

        var parts = body!.Token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public async Task Login_ReturnsSameUserIdAsRegistration()
    {
        var email = $"sameid-{Guid.NewGuid()}@test.com";
        const string password = "P@ssw0rd!";

        var registerResponse = await RegisterAsync(email, password);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        var loginResponse = await LoginAsync(email, password);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.Equal(registerBody!.Id, loginBody!.Id);
    }

    /// <summary>DTO used to deserialize auth endpoint responses.</summary>
    private sealed record AuthResponseDto(string? Id, string? Email, string? Token);
}
