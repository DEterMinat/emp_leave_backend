using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EmployeeLeaveApi.Helpers;
using Microsoft.Extensions.Configuration;

namespace EmployeeLeaveApi.Tests;

public class JwtHelperTests
{
    private readonly JwtHelper _jwtHelper;
    private readonly IConfiguration _configuration;

    public JwtHelperTests()
    {
        var inMemoryConfig = new Dictionary<string, string>
        {
            {"Jwt:Secret", "TestSecretKey123456789012345678901234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:ExpirationHours", "2"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig!)
            .Build();

        _jwtHelper = new JwtHelper(_configuration);
    }

    [Fact]
    public void GenerateToken_ShouldReturnNonEmptyToken()
    {
        // Act
        var token = _jwtHelper.GenerateToken("user-1", "testuser", "role-1", "Employee");

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ShouldContainCorrectClaims()
    {
        // Arrange
        var userId = "user-123";
        var username = "john.doe";
        var roleId = "role-456";
        var roleName = "Manager";

        // Act
        var token = _jwtHelper.GenerateToken(userId, username, roleId, roleName);

        // Assert - decode and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var nameIdClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
        var nameClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Name || c.Type == "unique_name");
        var roleIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "roleId");

        Assert.NotNull(nameIdClaim);
        Assert.Equal(userId, nameIdClaim.Value);
        Assert.NotNull(nameClaim);
        Assert.Equal(username, nameClaim.Value);
        Assert.NotNull(roleIdClaim);
        Assert.Equal(roleId, roleIdClaim.Value);
    }

    [Fact]
    public void GenerateToken_RoleName_ShouldBeLowercase()
    {
        // Act
        var token = _jwtHelper.GenerateToken("user1", "testuser", "role1", "ADMIN");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == "role" || c.Type == ClaimTypes.Role || c.Type.EndsWith("/role"));

        Assert.NotNull(roleClaim);
        Assert.Equal("admin", roleClaim.Value);
    }

    [Fact]
    public void GenerateToken_WithNullRoleName_ShouldNotContainRoleClaim()
    {
        // Act
        var token = _jwtHelper.GenerateToken("user1", "testuser", "role1", null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == "role" || c.Type == ClaimTypes.Role || c.Type.EndsWith("/role"));

        Assert.Null(roleClaim);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuer()
    {
        // Act
        var token = _jwtHelper.GenerateToken("user1", "testuser", "role1", "Employee");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectExpiration()
    {
        // Act
        var beforeGeneration = DateTime.UtcNow;
        var token = _jwtHelper.GenerateToken("user1", "testuser", "role1", "Employee");
        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Token should expire approximately 2 hours from now (as configured)
        var expectedExpiry = beforeGeneration.AddHours(2);
        Assert.True(jwtToken.ValidTo >= expectedExpiry.AddSeconds(-5));
        Assert.True(jwtToken.ValidTo <= afterGeneration.AddHours(2).AddSeconds(5));
    }

    [Fact]
    public void GenerateToken_ShouldContainJtiClaim()
    {
        // Act
        var token = _jwtHelper.GenerateToken("user1", "testuser", "role1", "Employee");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);

        Assert.NotNull(jtiClaim);
        Assert.True(Guid.TryParse(jtiClaim.Value, out _)); // Should be a valid GUID
    }

    [Fact]
    public void ValidateToken_ValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        var token = _jwtHelper.GenerateToken("user1", "testuser", "role1", "Employee");

        // Act
        var principal = _jwtHelper.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ShouldReturnNull()
    {
        // Act
        var principal = _jwtHelper.ValidateToken("this.is.not.a.valid.token");

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_EmptyToken_ShouldReturnNull()
    {
        // Act
        var principal = _jwtHelper.ValidateToken("");

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateToken_TwoTokens_ShouldHaveDifferentJti()
    {
        // Act
        var token1 = _jwtHelper.GenerateToken("user1", "testuser", "role1", "Employee");
        var token2 = _jwtHelper.GenerateToken("user1", "testuser", "role1", "Employee");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }
}
