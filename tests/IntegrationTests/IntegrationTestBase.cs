using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliverySystem.IntegrationTests.Infrastructure;

namespace DeliverySystem.IntegrationTests;

/// <summary>
/// Base class for integration tests that provides a pre-configured <see cref="HttpClient"/>
/// and convenience methods for common operations.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly DeliverySystemFactory Factory;
    protected readonly HttpClient Client;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTestBase"/> class.
    /// </summary>
    /// <param name="factory">The shared <see cref="DeliverySystemFactory"/>.</param>
    protected IntegrationTestBase(DeliverySystemFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Registers a test user and returns the HTTP response.
    /// </summary>
    /// <param name="email">The email to register.</param>
    /// <param name="password">The password to register.</param>
    /// <returns>The HTTP response from the register endpoint.</returns>
    protected Task<HttpResponseMessage> RegisterAsync(string email, string password)
    {
        return Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            CaptchaToken = "valid-captcha-token"
        });
    }

    /// <summary>
    /// Logs in a test user and returns the HTTP response.
    /// </summary>
    /// <param name="email">The email to log in with.</param>
    /// <param name="password">The password to log in with.</param>
    /// <returns>The HTTP response from the login endpoint.</returns>
    protected Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        return Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password,
            CaptchaToken = "valid-captcha-token"
        });
    }

    /// <summary>
    /// Registers a new user and returns a JWT token. Throws if registration fails.
    /// </summary>
    /// <param name="email">The email to register.</param>
    /// <param name="password">The password to register.</param>
    /// <returns>The JWT bearer token.</returns>
    protected async Task<string> GetTokenAsync(string email, string password)
    {
        var response = await RegisterAsync(email, password);
        if (!response.IsSuccessStatusCode)
        {
            // User may already exist — try logging in instead.
            response = await LoginAsync(email, password);
        }
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.Token!;
    }

    /// <summary>
    /// Returns the seeded admin JWT token.
    /// </summary>
    protected async Task<string> GetAdminTokenAsync()
        => await GetTokenForExistingUserAsync("admin@test.com", "Admin@Test1!");

    /// <summary>
    /// Logs in an existing user and returns their JWT token.
    /// </summary>
    private async Task<string> GetTokenForExistingUserAsync(string email, string password)
    {
        var response = await LoginAsync(email, password);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        return body!.Token!;
    }

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> with the given bearer token pre-set.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Resets fake services to their default state before each test.
    /// </summary>
    public Task InitializeAsync()
    {
        Factory.CaptchaService.ShouldPass = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record AuthResponseDto(string? Id, string? Email, string? Token);
}
