using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Infrastructure.Configuration;
using Warehouse.Auth.API.Services;
using Warehouse.Auth.DBModel.Models;

namespace Warehouse.Auth.API.Tests.Unit;

/// <summary>
/// Unit tests for JWT access token generation and refresh token generation.
/// </summary>
[TestFixture]
[Category("SDD-AUTH-001")]
public sealed class JwtTokenServiceTests
{
    private JwtTokenService _service = null!;
    private JwtSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new JwtSettings
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        _service = new JwtTokenService(Options.Create(_settings));
    }

    [Test]
    public void GenerateAccessToken_ReturnsNonEmptyToken()
    {
        User user = CreateTestUser();

        (string token, DateTime expiresAt) = _service.GenerateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityTokenHandler handler = new();
        bool canRead = handler.CanReadToken(token);
        canRead.Should().BeTrue();
    }

    [Test]
    public void GenerateAccessToken_ContainsSubClaim()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        string subClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        subClaim.Should().Be(user.Id.ToString());
    }

    [Test]
    public void GenerateAccessToken_ContainsUsernameClaim()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        string usernameClaim = jwt.Claims.First(c => c.Type == "username").Value;
        usernameClaim.Should().Be("testuser");
    }

    [Test]
    public void GenerateAccessToken_DoesNotContainEmailClaim()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        List<Claim> emailClaims = jwt.Claims.Where(c => c.Type == JwtRegisteredClaimNames.Email).ToList();
        emailClaims.Should().BeEmpty("JWT should contain only identity claims — no email");
    }

    [Test]
    public void GenerateAccessToken_DoesNotContainRoleClaims()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        List<string> roles = jwt.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        roles.Should().BeEmpty("JWT should not contain role claims — permissions are resolved at request time");
    }

    [Test]
    public void GenerateAccessToken_DoesNotContainPermissionClaims()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        List<Claim> permissionClaims = jwt.Claims.Where(c => c.Type == "permission").ToList();
        permissionClaims.Should().BeEmpty("JWT should not contain permission claims — permissions are resolved at request time");
    }

    [Test]
    public void GenerateAccessToken_ContainsOnlyIdentityClaims()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        HashSet<string> allowedClaimTypes = new(["sub", "username", "jti", "iat", "exp", "iss", "aud", "nbf"]);
        List<string> unexpectedClaims = jwt.Claims
            .Where(c => !allowedClaimTypes.Contains(c.Type))
            .Select(c => c.Type)
            .ToList();
        unexpectedClaims.Should().BeEmpty("JWT should contain only identity claims (sub, username, jti, iat)");
    }

    [Test]
    public void GenerateAccessToken_ContainsJtiClaim()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        string jtiClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        jtiClaim.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void GenerateAccessToken_JtiIsUniquePerCall()
    {
        User user = CreateTestUser();

        (string token1, DateTime _) = _service.GenerateAccessToken(user);
        (string token2, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(token1);
        JwtSecurityToken jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(token2);

        string jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        string jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    [Test]
    public void GenerateAccessToken_ExpiresAtIsInFuture()
    {
        User user = CreateTestUser();

        (string _, DateTime expiresAt) = _service.GenerateAccessToken(user);

        expiresAt.Should().BeAfter(DateTime.UtcNow);
        expiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            TimeSpan.FromSeconds(5));
    }

    [Test]
    public void GenerateAccessToken_HasCorrectIssuer()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("TestIssuer");
    }

    [Test]
    public void GenerateAccessToken_HasCorrectAudience()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Test]
    public void GenerateAccessToken_SignatureIsValid()
    {
        User user = CreateTestUser();

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityTokenHandler handler = new();
        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        ClaimsPrincipal principal = handler.ValidateToken(token, parameters, out SecurityToken _);
        principal.Should().NotBeNull();
    }

    [Test]
    public void GenerateAccessToken_UserWithNoRoles_HasNoRoleClaims()
    {
        User user = new()
        {
            Id = 1,
            Username = "noroles",
            Email = "noroles@test.local",
            PasswordHash = "hash",
            FirstName = "No",
            LastName = "Roles",
            UserRoles = []
        };

        (string token, DateTime _) = _service.GenerateAccessToken(user);

        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        List<Claim> roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().BeEmpty();
    }

    [Test]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        string refreshToken = _service.GenerateRefreshToken();

        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void GenerateRefreshToken_IsBase64()
    {
        string refreshToken = _service.GenerateRefreshToken();

        byte[] bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().HaveCount(64);
    }

    [Test]
    public void GenerateRefreshToken_IsUniquePerCall()
    {
        string token1 = _service.GenerateRefreshToken();
        string token2 = _service.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    private static User CreateTestUser()
    {
        Role adminRole = new()
        {
            Id = 1,
            Name = "Admin",
            IsSystem = true
        };

        User user = new()
        {
            Id = 42,
            Username = "testuser",
            Email = "test@warehouse.local",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            UserRoles =
            [
                new UserRole { UserId = 42, RoleId = 1, Role = adminRole }
            ]
        };

        return user;
    }
}
