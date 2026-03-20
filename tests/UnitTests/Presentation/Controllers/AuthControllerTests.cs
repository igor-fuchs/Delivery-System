using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace DeliverySystem.UnitTests.Presentation.Controllers;

public sealed class AuthControllerTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService);
    }

    #region Register

    [Fact]
    public async Task Register_Success_ShouldReturnOkWithAuthResponse()
    {
        var request = new RegisterRequest("user@example.com", "Str0ng!Pass", "captcha");
        var expected = new AuthResponse("id-1", "user@example.com", "jwt");

        _authService.RegisterAsync(request).Returns(expected);

        var result = await _sut.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task Register_ShouldDelegateToAuthService()
    {
        var request = new RegisterRequest("user@example.com", "Str0ng!Pass", "captcha");

        _authService.RegisterAsync(request).Returns(new AuthResponse("id", "e", "t"));

        await _sut.Register(request);

        await _authService.Received(1).RegisterAsync(request);
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_Success_ShouldReturnOkWithAuthResponse()
    {
        var request = new LoginRequest("user@example.com", "Str0ng!Pass", "captcha");
        var expected = new AuthResponse("id-1", "user@example.com", "jwt");

        _authService.LoginAsync(request).Returns(expected);

        var result = await _sut.Login(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task Login_ShouldDelegateToAuthService()
    {
        var request = new LoginRequest("user@example.com", "Str0ng!Pass", "captcha");

        _authService.LoginAsync(request).Returns(new AuthResponse("id", "e", "t"));

        await _sut.Login(request);

        await _authService.Received(1).LoginAsync(request);
    }

    #endregion

    #region GoogleLogin

    [Fact]
    public async Task GoogleLogin_Success_ShouldReturnOkWithAuthResponse()
    {
        var request = new GoogleLoginRequest("google-id-token");
        var expected = new AuthResponse("id-1", "google@gmail.com", "jwt");

        _authService.GoogleLoginAsync(request).Returns(expected);

        var result = await _sut.GoogleLogin(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GoogleLogin_ShouldDelegateToAuthService()
    {
        var request = new GoogleLoginRequest("google-id-token");

        _authService.GoogleLoginAsync(request).Returns(new AuthResponse("id", "e", "t"));

        await _sut.GoogleLogin(request);

        await _authService.Received(1).GoogleLoginAsync(request);
    }

    #endregion
}
