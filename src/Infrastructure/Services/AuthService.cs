using System.Text.Encodings.Web;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Identity-based implementation of <see cref="IAuthService"/>.
/// Uses <see cref="UserManager{TUser}"/> for user creation, password verification,
/// and <see cref="ITokenService"/> for JWT generation.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="tokenService">Service for JWT generation.</param>
    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    /// <inheritdoc />
    /// <exception cref="ConflictException">Thrown when a user with the same email already exists.</exception>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = HtmlEncoder.Default.Encode(request.Email);

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new ConflictException("User already exists.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())
                as IReadOnlyDictionary<string, string[]>;

            throw new ValidationException(errors);
        }

        var token = _tokenService.GenerateToken(user.Id, user.Email!);
        return new AuthResponse(user.Id.ToString(), user.Email!, token);
    }

    /// <inheritdoc />
    /// <exception cref="UnauthorizedAccessException">Thrown when the email is not found or the password is incorrect.</exception>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = HtmlEncoder.Default.Encode(request.Email);

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = _tokenService.GenerateToken(user.Id, user.Email!);
        return new AuthResponse(user.Id.ToString(), user.Email!, token);
    }
}
