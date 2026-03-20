using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Data;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Google OAuth2 implementation of <see cref="IGoogleAuthService"/>.
/// Validates the Google ID token, provisions the user via ASP.NET Core Identity
/// if it does not already exist, and returns a JWT through <see cref="ITokenService"/>.
/// </summary>
public sealed class GoogleAuthService : IGoogleAuthService
{
    private const string GoogleProvider = "Google";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly GoogleOptions _googleOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleAuthService"/> class.
    /// </summary>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="tokenService">Service for JWT generation.</param>
    /// <param name="googleOptions">Google OAuth2 configuration options.</param>
    public GoogleAuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IOptions<GoogleOptions> googleOptions)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _googleOptions = googleOptions.Value;
    }

    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">Thrown when the Google ID token is invalid or expired.</exception>
    public async Task<AuthResponse> LoginAsync(GoogleLoginRequest request)
    {
        var payload = await ValidateIdTokenAsync(request.IdToken);

        var user = await _userManager.FindByEmailAsync(payload.Email);

        if (user is null)
        {
            user = await CreateUserAsync(payload);
        }
        else
        {
            await EnsureGoogleLoginLinkedAsync(user, payload);
        }

        var token = _tokenService.GenerateToken(user.Id, user.Email!);
        return new AuthResponse(user.Id.ToString(), user.Email!, token);
    }

    /// <summary>
    /// Validates the Google ID token against Google's public keys and the configured client ID.
    /// </summary>
    private async Task<GoogleJsonWebSignature.Payload> ValidateIdTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleOptions.ClientId]
            };

            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Invalid Google ID token.");
        }
    }

    /// <summary>
    /// Creates a new Identity user from the Google payload and links the Google external login.
    /// </summary>
    private async Task<ApplicationUser> CreateUserAsync(GoogleJsonWebSignature.Payload payload)
    {
        var user = new ApplicationUser
        {
            UserName = payload.Email,
            Email = payload.Email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        var loginInfo = new UserLoginInfo(GoogleProvider, payload.Subject, GoogleProvider);
        await _userManager.AddLoginAsync(user, loginInfo);

        return user;
    }

    /// <summary>
    /// Ensures the Google external login is linked to an existing user.
    /// If the user was originally created via email/password and now logs in with Google,
    /// the Google login is linked automatically.
    /// </summary>
    private async Task EnsureGoogleLoginLinkedAsync(ApplicationUser user, GoogleJsonWebSignature.Payload payload)
    {
        var logins = await _userManager.GetLoginsAsync(user);
        var hasGoogleLogin = logins.Any(l => l.LoginProvider == GoogleProvider);

        if (!hasGoogleLogin)
        {
            var loginInfo = new UserLoginInfo(GoogleProvider, payload.Subject, GoogleProvider);
            await _userManager.AddLoginAsync(user, loginInfo);
        }
    }
}
