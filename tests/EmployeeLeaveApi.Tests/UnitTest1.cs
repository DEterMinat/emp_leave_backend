using EmployeeLeaveApi.Helpers;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace EmployeeLeaveApi.Tests;

public class JwtAuthTests
{
    [Fact]
    public void GenerateToken_ShouldLowercaseRoleName()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string> {
            {"Jwt:Secret", "TestSecretKey123456789012345678901234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:ExpirationHours", "1"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig!)
            .Build();

        var jwtHelper = new JwtHelper(configuration);
        var roleName = "ADMIN"; // Uppercase role

        // Act
        var token = jwtHelper.GenerateToken("user1", "testuser", "role1", roleName);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // Find the role claim
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type.EndsWith("/role"));
        
        Assert.NotNull(roleClaim);
        Assert.Equal("admin", roleClaim.Value); // Should be lowercase
    }
}
