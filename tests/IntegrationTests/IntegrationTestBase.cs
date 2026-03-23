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
    /// Resets the fake CAPTCHA service to always pass before each test.
    /// </summary>
    public Task InitializeAsync()
    {
        Factory.CaptchaService.ShouldPass = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;
}
