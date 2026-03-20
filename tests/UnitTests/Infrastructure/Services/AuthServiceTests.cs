using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute.ReturnsExtensions;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

public sealed class AuthServiceTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthService _sut;

    private static readonly GoogleOptions TestGoogleOptions = new()
    {
        WebClientId = "web-client-id",
        MobileClientId = "mobile-client-id"
    };

    public AuthServiceTests()
    {
        // UserManager requires a non-null IUserStore<T>.
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);

        _tokenService = Substitute.For<ITokenService>();
        _captchaService = Substitute.For<ICaptchaService>();
        _logger = Substitute.For<ILogger<AuthService>>();

        // Default: CAPTCHA always passes.
        _captchaService.ValidateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Default: token generation returns a predictable value.
        _tokenService.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("jwt-token");

        _sut = new AuthService(
            _userManager,
            _tokenService,
            _captchaService,
            Options.Create(TestGoogleOptions),
            _logger);
    }

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_ValidRequest_ShouldReturnAuthResponse()
    {
        var request = new RegisterRequest("user@example.com", "Str0ng!Pass", "captcha");

        _userManager.FindByEmailAsync("user@example.com").ReturnsNull();
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Success);

        var result = await _sut.RegisterAsync(request);

        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("jwt-token", result.Token);
        Assert.NotEmpty(result.Id);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowConflictException()
    {
        var request = new RegisterRequest("dup@example.com", "Str0ng!Pass", "captcha");

        _userManager.FindByEmailAsync("dup@example.com")
            .Returns(new ApplicationUser { Email = "dup@example.com" });

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_IdentityFailure_ShouldThrowValidationException()
    {
        var request = new RegisterRequest("user@example.com", "weak", "captcha");
        var identityErrors = new[] { new IdentityError { Code = "PasswordTooShort", Description = "Password too short." } };

        _userManager.FindByEmailAsync("user@example.com").ReturnsNull();
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Failed(identityErrors));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.RegisterAsync(request));

        Assert.Contains("PasswordTooShort", ex.Errors.Keys);
        Assert.Contains("Password too short.", ex.Errors["PasswordTooShort"]);
    }

    [Fact]
    public async Task RegisterAsync_CaptchaFails_ShouldThrowUnauthorizedAccessException()
    {
        var request = new RegisterRequest("user@example.com", "Str0ng!Pass", "bad-captcha");

        _captchaService.ValidateAsync("bad-captcha", Arg.Any<CancellationToken>())
            .Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RegisterAsync(request));

        Assert.Equal("CAPTCHA verification failed.", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_ShouldTrimEmail()
    {
        var request = new RegisterRequest("  user@example.com  ", "Str0ng!Pass", "captcha");

        _userManager.FindByEmailAsync("user@example.com").ReturnsNull();
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Success);

        var result = await _sut.RegisterAsync(request);

        Assert.Equal("user@example.com", result.Email);
        await _userManager.Received(1).FindByEmailAsync("user@example.com");
    }

    #endregion

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        var request = new LoginRequest("user@example.com", "Str0ng!Pass", "captcha");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com"
        };

        _userManager.FindByEmailAsync("user@example.com").Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(true);

        var result = await _sut.LoginAsync(request);

        Assert.Equal(user.Id.ToString(), result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.Equal("jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ShouldThrowUnauthorizedAccessException()
    {
        var request = new LoginRequest("unknown@example.com", "any", "captcha");

        _userManager.FindByEmailAsync("unknown@example.com").ReturnsNull();

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(request));

        Assert.Equal("Invalid credentials.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedAccessException()
    {
        var request = new LoginRequest("user@example.com", "wrong", "captcha");
        var user = new ApplicationUser { Email = "user@example.com" };

        _userManager.FindByEmailAsync("user@example.com").Returns(user);
        _userManager.CheckPasswordAsync(user, "wrong").Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(request));

        Assert.Equal("Invalid credentials.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_CaptchaFails_ShouldThrowUnauthorizedAccessException()
    {
        var request = new LoginRequest("user@example.com", "Str0ng!Pass", "bad");

        _captchaService.ValidateAsync("bad", Arg.Any<CancellationToken>())
            .Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(request));

        Assert.Equal("CAPTCHA verification failed.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_CaptchaFails_ShouldNotCallUserManager()
    {
        var request = new LoginRequest("user@example.com", "Str0ng!Pass", "bad");

        _captchaService.ValidateAsync("bad", Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginAsync(request));

        await _userManager.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
    }

    #endregion
}
