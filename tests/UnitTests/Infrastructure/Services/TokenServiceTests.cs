using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeliverySystem.Application.Options;
using DeliverySystem.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

public sealed class TokenServiceTests
{
    private static readonly JwtOptions DefaultOptions = new()
    {
        SecretKey = "super-secret-key-minimum-32-characters!!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpirationMinutes = 30
    };

    private readonly TokenService _sut = new(Options.Create(DefaultOptions));

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyString()
    {
        var token = _sut.GenerateToken(Guid.NewGuid(), "user@example.com", "user");

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_ShouldContainSubClaim()
    {
        var userId = Guid.NewGuid();

        var token = _sut.GenerateToken(userId, "user@example.com", "user");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
    }

    [Fact]
    public void GenerateToken_ShouldContainEmailClaim()
    {
        const string email = "user@example.com";

        var token = _sut.GenerateToken(Guid.NewGuid(), email, "user");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public void GenerateToken_ShouldContainJtiClaim()
    {
        var token = _sut.GenerateToken(Guid.NewGuid(), "user@example.com", "user");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var jti = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.True(Guid.TryParse(jti, out _));
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectIssuer()
    {
        var token = _sut.GenerateToken(Guid.NewGuid(), "user@example.com", "user");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(DefaultOptions.Issuer, jwt.Issuer);
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectAudience()
    {
        var token = _sut.GenerateToken(Guid.NewGuid(), "user@example.com", "user");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(DefaultOptions.Audience, jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_ShouldSetExpirationBasedOnOptions()
    {
        var beforeGenerate = DateTime.UtcNow;

        var token = _sut.GenerateToken(Guid.NewGuid(), "user@example.com", "user");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedExpiry = beforeGenerate.AddMinutes(DefaultOptions.ExpirationMinutes);

        // Allow a 5-second tolerance for test execution time.
        Assert.InRange(jwt.ValidTo, expectedExpiry.AddSeconds(-5), expectedExpiry.AddSeconds(5));
    }

    [Fact]
    public void GenerateToken_ShouldGenerateUniqueJtiPerCall()
    {
        var token1 = _sut.GenerateToken(Guid.NewGuid(), "user@example.com", "user");
        var token2 = _sut.GenerateToken(Guid.NewGuid(), "user@example.com", "user");

        var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(token1);
        var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(token2);

        var jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }
}
