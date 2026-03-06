using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeLeaveApi.Helpers;

public class JwtHelper
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly int _expirationHours;

    public JwtHelper(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"] ?? "DefaultSecretKey123456789012345678901234567890";
        _issuer = configuration["Jwt:Issuer"] ?? "EmployeeLeaveApi";
        _expirationHours = int.Parse(configuration["Jwt:ExpirationHours"] ?? "24");
    }

    /// <summary>
    /// Generate JWT token for user
    /// </summary>
    public string GenerateToken(string userId, string username, string roleId, string? roleName = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new("roleId", roleId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(roleName))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName.ToLower()));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
