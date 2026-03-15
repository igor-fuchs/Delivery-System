using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Domain.Entities;

namespace DeliverySystem.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

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

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = _tokenService.GenerateToken(user);
        return new AuthResponse(user.Id.ToString(), user.Email, token);
    }
}
