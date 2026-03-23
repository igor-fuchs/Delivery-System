using System.Net;
using System.Net.Http.Json;
using DeliverySystem.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace DeliverySystem.IntegrationTests.Auth;

/// <summary>
/// End-to-end tests for the <c>POST /api/auth/google</c> endpoint.
/// Since Google ID token validation requires a real Google-signed JWT,
/// these tests verify validation errors and invalid token rejection.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class GoogleLoginEndpointTests : IntegrationTestBase
{

    private readonly ITestOutputHelper _output;

    public GoogleLoginEndpointTests(DeliverySystemFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task GoogleLogin_EmptyToken_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/google", new
        {
            Token = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_InvalidToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/google", new
        {
            Token = "not-a-valid-google-id-token"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_NullBody_ReturnsBadRequest()
    {
        var response = await Client.PostAsync("/api/auth/google",
            JsonContent.Create(new { Token = (string?)null }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
