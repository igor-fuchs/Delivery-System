using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Application.Services;

/// <summary>
/// Application service responsible for user authentication (registration and login).
/// </summary>
public sealed class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="userRepository">Repository for user persistence.</param>
    /// <param name="tokenService">Service for JWT generation.</param>
    /// <param name="passwordHasher">Service for password hashing and verification.</param>
    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Registers a new user, hashes the password, persists the user, and returns a JWT.
    /// </summary>
    /// <param name="request">The registration data.</param>
    /// <returns>An <see cref="AuthResponse"/> containing the user ID, email, and JWT token.</returns>
    /// <exception cref="ConflictException">Thrown when a user with the same email already exists.</exception>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null) throw new ConflictException("User already exists.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Name, request.Email, hash);
        await _userRepository.AddAsync(user);

        var token = _tokenService.GenerateToken(user);
        return new AuthResponse(user.Id.ToString(), user.Email, token);
    }

    /// <summary>
    /// Authenticates a user by email and password, and returns a JWT on success.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    /// <returns>An <see cref="AuthResponse"/> containing the user ID, email, and JWT token.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the email is not found or the password is incorrect.</exception>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = _tokenService.GenerateToken(user);
        return new AuthResponse(user.Id.ToString(), user.Email, token);
    }
}
