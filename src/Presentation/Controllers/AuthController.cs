using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DeliverySystem.Application.Options;
using Microsoft.AspNetCore.Cors;

namespace DeliverySystem.Presentation.Controllers;

/// <summary>
/// API controller for authentication endpoints (register, login, and Google OAuth2).
/// All endpoints are public (no authentication required) and rate-limited by client IP address.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
[Produces("application/json")]
[EnableRateLimiting(RateLimitOptions.AuthPolicyName)]
[EnableCors(CorsOptions.AuthPolicyName)]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration payload containing email, password and captcha token.</param>
    /// <returns>An <see cref="AuthResponse"/> with the user ID, email, and JWT token.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="409">A user with the same email already exists.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates an existing user.
    /// </summary>
    /// <param name="request">The login payload containing email, password, and captcha token.</param>
    /// <returns>An <see cref="AuthResponse"/> with the user ID, email, and JWT token.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Invalid password or CAPTCHA failed.</response>
    /// <response code="404">No account found for the supplied email address.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user via Google OAuth2 ID token.
    /// Creates the user automatically if they don't already exist.
    /// Supports both web and mobile clients.
    /// </summary>
    /// <param name="request">The Google login payload containing the ID token from Google Sign-In.</param>
    /// <returns>An <see cref="AuthResponse"/> with the user ID, email, and JWT token.</returns>
    /// <response code="200">Google login successful.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Invalid or expired Google ID token.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var response = await _authService.GoogleLoginAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Initiates a password reset by sending a reset link to the user's email address.
    /// </summary>
    /// <param name="request">The payload containing the email, captcha token, and callback URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Reset email dispatched (or silently suppressed for unknown addresses).</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">CAPTCHA verification failed.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(request, ct);
        return Ok();
    }

    /// <summary>
    /// Resets the user's password using the Identity-generated token received in the reset email.
    /// </summary>
    /// <param name="request">The payload containing userId, reset token, new password, and captcha token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Invalid or expired reset token, unknown userId, or CAPTCHA failed.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return Ok();
    }
}
