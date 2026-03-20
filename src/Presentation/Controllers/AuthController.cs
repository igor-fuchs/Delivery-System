using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DeliverySystem.Application.Options;
using Microsoft.AspNetCore.Cors;

namespace DeliverySystem.Presentation.Controllers;

/// <summary>
/// API controller for authentication endpoints (register and login).
/// Rate-limited by client IP address.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitOptions.AuthPolicyName)]
[EnableCors(CorsOptions.PolicyName)]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IGoogleAuthService _googleAuthService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="googleAuthService">The Google OAuth2 authentication service.</param>
    public AuthController(IAuthService authService, IGoogleAuthService googleAuthService)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration payload containing name, email, and password.</param>
    /// <returns>An <see cref="AuthResponse"/> with the user ID, email, and JWT token.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="409">A user with the same email already exists.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates an existing user.
    /// </summary>
    /// <param name="request">The login payload containing email and password.</param>
    /// <returns>An <see cref="AuthResponse"/> with the user ID, email, and JWT token.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Invalid email or password.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user via a Google ID token.
    /// If the user does not exist, a new account is created and linked to Google.
    /// </summary>
    /// <param name="request">The request payload containing the Google ID token obtained from the client-side OAuth2 flow.</param>
    /// <returns>An <see cref="AuthResponse"/> with the user ID, email, and JWT token.</returns>
    /// <response code="200">Google login successful.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Invalid or expired Google ID token.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var response = await _googleAuthService.LoginAsync(request);
        return Ok(response);
    }
}
