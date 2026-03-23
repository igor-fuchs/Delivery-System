using System.Net;
using System.Net.Http.Json;
using DeliverySystem.IntegrationTests.Infrastructure;

namespace DeliverySystem.IntegrationTests.Auth;

/// <summary>
/// End-to-end tests for the <c>POST /api/auth/register</c> endpoint.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class RegisterEndpointTests : IntegrationTestBase
{
    public RegisterEndpointTests(DeliverySystemFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithAuthResponse()
    {
        var response = await RegisterAsync($"register-ok-{Guid.NewGuid()}@test.com", "P@ssw0rd!");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.NotNull(body!.Id);
        Assert.NotNull(body.Email);
        Assert.NotNull(body.Token);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = $"dup-{Guid.NewGuid()}@test.com";
        await RegisterAsync(email, "P@ssw0rd!");

        var response = await RegisterAsync(email, "P@ssw0rd!");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var response = await RegisterAsync("not-an-email", "P@ssw0rd!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        var response = await RegisterAsync($"weak-{Guid.NewGuid()}@test.com", "123");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyBody_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "",
            Password = "",
            CaptchaToken = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingCaptcha_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"nocaptcha-{Guid.NewGuid()}@test.com",
            Password = "P@ssw0rd!",
            CaptchaToken = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_CaptchaFails_ReturnsUnauthorized()
    {
        Factory.CaptchaService.ShouldPass = false;

        var response = await RegisterAsync($"captcha-fail-{Guid.NewGuid()}@test.com", "P@ssw0rd!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsValidJwtToken()
    {
        var response = await RegisterAsync($"jwt-{Guid.NewGuid()}@test.com", "P@ssw0rd!");

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body?.Token);

        // JWT tokens have 3 base64-encoded parts separated by dots
        var parts = body!.Token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    /// <summary>DTO used to deserialize auth endpoint responses.</summary>
    private sealed record AuthResponseDto(string? Id, string? Email, string? Token);
}
