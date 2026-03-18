using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DeliverySystem.Presentation.Controllers;

/// <summary>
/// API controller for authentication endpoints (register and login).
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    /// <param name="request">The registration payload containing name, email, and password.</param>
    /// <returns>An <see cref="AuthResponse"/> with the user ID, email, and JWT token.</returns>
    /// <response code="200">User registered successfully.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="409">A user with the same email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }
}
