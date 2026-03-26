using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Domain.Constants;
using DeliverySystem.Infrastructure.Identity;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Identity-based implementation of <see cref="IAuthService"/>.
/// Handles credential-based registration and login via ASP.NET Core Identity,
/// as well as federated login via Google OAuth2 ID token validation.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICaptchaService _captchaService;
    private readonly GoogleOptions _googleOptions;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="tokenService">Service for JWT generation.</param>
    /// <param name="captchaService">Service for CAPTCHA token verification.</param>
    /// <param name="googleOptions">Google OAuth2 configuration options.</param>
    /// <param name="logger">Logger for security-relevant events.</param>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ICaptchaService captchaService,
        IOptions<GoogleOptions> googleOptions,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _captchaService = captchaService;
        _googleOptions = googleOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="ConflictException">Thrown when a user with the same email already exists.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when CAPTCHA verification fails.</exception>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        await ValidateCaptchaAsync(request.CaptchaToken);

        var email = request.Email.Trim().ToLowerInvariant();

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            _logger.LogWarning("Registration attempt with duplicate email");
            throw new ConflictException("User already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw new ValidationException(BuildValidationErrors(result.Errors));

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        _logger.LogInformation("New user registered with ID {UserId}", user.Id);

        var token = _tokenService.GenerateToken(user.Id, email, AppRoles.User);
        return new AuthResponse(user.Id.ToString(), email, token);
    }

    /// <inheritdoc />
    /// <exception cref="NotFoundException">Thrown when no account exists for the supplied email address.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the password is incorrect or CAPTCHA verification fails.</exception>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        await ValidateCaptchaAsync(request.CaptchaToken);

        // Trim whitespace and convert to lowercase — same rationale as RegisterAsync.
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for unknown email {Email}", email);
            throw new NotFoundException($"No account found for '{email}'.");
        }

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            _logger.LogWarning("Failed login attempt for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRoles.User;

        var token = _tokenService.GenerateToken(user.Id, user.Email!, role);
        return new AuthResponse(user.Id.ToString(), user.Email!, token);
    }

    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the Google ID token is invalid, expired, or the associated email is not verified by Google.
    /// </exception>
    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleOptions.WebClientId]
            };

            payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, settings);
        }
        catch (InvalidJwtException)
        {
            _logger.LogWarning("Google login attempt with invalid ID token");
            throw new UnauthorizedAccessException("Invalid Google ID token.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Google JWKS endpoint");
            throw new ServiceUnavailableException("Failed to reach Google JWKS endpoint.");
        }

        // Reject tokens where Google has not verified the email address.
        if (!payload.EmailVerified)
        {
            _logger.LogWarning("Google login rejected — email not verified by Google");
            throw new UnauthorizedAccessException("Google account email is not verified.");
        }

        // The email claim is normally guaranteed for Google accounts, but guard
        // against malformed tokens that omit it to avoid a NullReferenceException.
        var email = payload.Email
            ?? throw new UnauthorizedAccessException("Google ID token does not contain an email claim.");

        var user = await _userManager.FindByEmailAsync(email);

        // Auto-register the user on first Google login.
        // EmailConfirmed is set to true because Google has already verified ownership of the address.
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
                throw new ValidationException(BuildValidationErrors(result.Errors));

            await _userManager.AddToRoleAsync(user, AppRoles.User);
        }

        _logger.LogInformation("Google login succeeded for user {UserId}", user.Id);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? AppRoles.User;

        var token = _tokenService.GenerateToken(user.Id, user.Email!, role);
        return new AuthResponse(user.Id.ToString(), user.Email!, token);
    }

    /// <summary>
    /// Validates the CAPTCHA token and throws if it is invalid.
    /// Centralises the check so callers cannot accidentally ignore the boolean result.
    /// </summary>
    /// <param name="captchaToken">The CAPTCHA token obtained from the client.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the CAPTCHA token is invalid.</exception>
    private async Task ValidateCaptchaAsync(string captchaToken)
    {
        var isValid = await _captchaService.ValidateAsync(captchaToken);
        if (!isValid)
        {
            _logger.LogWarning("CAPTCHA verification failed");
            throw new UnauthorizedAccessException("CAPTCHA verification failed.");
        }
    }

    /// <summary>
    /// Converts a collection of <see cref="IdentityError"/> into a read-only dictionary
    /// keyed by error code, as required by <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="errors">The Identity errors returned by a failed operation.</param>
    /// <returns>A dictionary mapping each error code to its associated messages.</returns>
    private static IReadOnlyDictionary<string, string[]> BuildValidationErrors(
        IEnumerable<IdentityError> errors) =>
        errors
            .GroupBy(e => e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
