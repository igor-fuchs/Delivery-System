using DeliverySystem.Application.Interfaces;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace DeliverySystem.IntegrationTests.Infrastructure;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces production services
/// with test-friendly alternatives: SQLite in-memory database, fake CAPTCHA service,
/// fake email service, in-memory distributed cache, and elevated rate limits.
/// </summary>
public sealed class DeliverySystemFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    /// <summary>
    /// Gets the shared <see cref="FakeCaptchaService"/> instance used by all tests
    /// so individual tests can toggle CAPTCHA pass/fail behavior.
    /// </summary>
    public FakeCaptchaService CaptchaService { get; } = new();

    /// <summary>
    /// Gets the shared <see cref="TestFakeEmailService"/> instance used by all tests
    /// to capture sent emails and verify email dispatch behavior.
    /// </summary>
    public TestFakeEmailService EmailService { get; } = new();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting injects values early enough for Program.cs to read them
        // during service registration (e.g. JWT bearer and CORS configuration
        // which are consumed eagerly before ConfigureAppConfiguration runs).
        builder.UseSetting("Jwt:SecretKey", "integration-test-secret-key-minimum-32-chars!!");
        builder.UseSetting("Jwt:Issuer", "DeliverySystem.Tests");
        builder.UseSetting("Jwt:Audience", "DeliverySystem.Tests");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
        builder.UseSetting("Cors:AuthAllowedOrigins:0", "http://localhost");
        builder.UseSetting("Cors:AuthAllowedMethods:0", "POST");

        // Redis — UseSetting so options validation passes during startup.
        // The actual connection is replaced in ConfigureServices below.
        builder.UseSetting("Redis:ConnectionString", "localhost:6379");
        builder.UseSetting("Redis:InstanceName", "Test:");
        builder.UseSetting("Redis:ProductCacheTtlMinutes", "10");

        // Rate limiting — UseSetting so values are available when AddAuthRateLimiter
        // reads configuration eagerly during Program.cs service registration.
        builder.UseSetting("RateLimit:AuthPermitLimit", "10000");
        builder.UseSetting("RateLimit:AuthWindowMinutes", "1");
        builder.UseSetting("RateLimit:GlobalPermitLimit", "10000");
        builder.UseSetting("RateLimit:GlobalWindowMinutes", "1");
        builder.UseSetting("RateLimit:ProductsPermitLimit", "10000");
        builder.UseSetting("RateLimit:ProductsWindowMinutes", "1");
        builder.UseSetting("RateLimit:OrdersPermitLimit", "10000");
        builder.UseSetting("RateLimit:OrdersWindowMinutes", "1");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Database (overridden at DbContext level, but options validation requires it)
                ["Database:ConnectionString"] = "Data Source=:memory:",

                // reCAPTCHA
                ["Recaptcha:SecretKey"] = "test-recaptcha-key",
                ["Recaptcha:MinimumScore"] = "0.5",

                // Google
                ["Google:WebClientId"] = "test-google-client-id",

                // Admin Seed
                ["AdminSeed:Email"] = "admin@test.com",
                ["AdminSeed:Password"] = "Admin@Test1!",

                // Resend (options validation requires values; actual sending is replaced by TestFakeEmailService)
                ["Resend:ApiKey"] = "test-resend-api-key",
                ["Resend:FromEmail"] = "noreply@test.com",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations to avoid
            // SQL Server / SQLite provider conflict in EF Core's internal service provider.
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();

            // Remove internal EF Core configuration services that carry the UseSqlServer call
            var efCoreDescriptors = services
                .Where(d => d.ServiceType.IsGenericType
                         && d.ServiceType.GenericTypeArguments.Contains(typeof(ApplicationDbContext)))
                .ToList();

            foreach (var descriptor in efCoreDescriptors)
                services.Remove(descriptor);

            // Register SQLite in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Replace CAPTCHA service with controllable fake
            var captchaDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICaptchaService));
            if (captchaDescriptor is not null)
                services.Remove(captchaDescriptor);

            services.AddSingleton<ICaptchaService>(CaptchaService);

            // Replace email service with test fake that captures sent emails.
            // RemoveAll handles both direct registration and AddHttpClient<IEmailService,...> registration.
            services.RemoveAll<IEmailService>();

            services.AddSingleton<IEmailService>(EmailService);

            // Replace Redis distributed cache with in-memory implementation
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Remove Redis connection so rate limiters fall back to in-memory
            services.RemoveAll<IConnectionMultiplexer>();

            // Switch Data Protection to ephemeral (in-memory) keys.
            // The production setup persists keys to Redis via PersistKeysToStackExchangeRedis,
            // which fails in tests because Redis is removed above. Ephemeral keys are fine for
            // tests — they only need to survive the lifetime of a single test run.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    /// <summary>
    /// Opens the SQLite connection, creates the database schema, and seeds roles and admin user before any test runs.
    /// </summary>
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed roles and admin user using the same seeder as production.
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    /// <summary>
    /// Closes the SQLite connection, destroying the in-memory database.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}

/// <summary>
/// Test-only implementation of <see cref="IEmailService"/> that records all sent emails
/// in memory so tests can assert on email dispatch behavior.
/// </summary>
public sealed class TestFakeEmailService : IEmailService
{
    private readonly List<SentEmail> _sentEmails = [];

    /// <summary>Gets the list of emails sent during the current test.</summary>
    public IReadOnlyList<SentEmail> SentEmails => _sentEmails;

    /// <summary>Clears the captured email list between tests.</summary>
    public void Reset() => _sentEmails.Clear();

    /// <inheritdoc />
    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        _sentEmails.Add(new SentEmail(toEmail, userName, resetUrl));
        return Task.CompletedTask;
    }

    /// <summary>Represents a captured outgoing email.</summary>
    /// <param name="ToEmail">The recipient address.</param>
    /// <param name="UserName">The display name passed to the email template.</param>
    /// <param name="ResetUrl">The full password reset URL included in the email.</param>
    public sealed record SentEmail(string ToEmail, string UserName, string ResetUrl);
}
