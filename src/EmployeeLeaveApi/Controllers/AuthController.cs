using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Helpers;
using EmployeeLeaveApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMongoDbContext _context;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMongoDbContext context, JwtHelper jwtHelper, ILogger<AuthController> logger)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _logger = logger;
    }

    /// <summary>
    /// Login and get JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("🔐 Login attempt for user: {Username}", request.Username);

        var user = await _context.Users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
        
        if (user == null)
        {
            _logger.LogWarning("❌ Login failed - User not found: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Verify password
        if (!PasswordHelper.VerifyPassword(request.Password, user.Password))
        {
            _logger.LogWarning("❌ Login failed - Invalid password for user: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Get role
        var role = await _context.Roles.Find(r => r.Id == user.RoleId).FirstOrDefaultAsync();

        // Generate token
        var token = _jwtHelper.GenerateToken(user.Id!, user.Username, user.RoleId, role?.RoleName);

        _logger.LogInformation("✅ Login successful for user: {Username}", request.Username);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = user.Id!,
            Username = user.Username,
            RoleId = user.RoleId,
            RoleName = role?.RoleName
        });
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("📝 Registration attempt for user: {Username}", request.Username);

        // Check if username exists
        var existing = await _context.Users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
        if (existing != null)
        {
            _logger.LogWarning("❌ Registration failed - Username exists: {Username}", request.Username);
            return BadRequest(new { message = "Username already exists" });
        }

        // Validate Role (Support both ID and Name)
        string roleId = request.RoleId;
        bool isObjectId = MongoDB.Bson.ObjectId.TryParse(request.RoleId, out _);

        if (isObjectId)
        {
            // Verify if role exists
            var roleExists = await _context.Roles.Find(r => r.Id == request.RoleId).AnyAsync();
            if (!roleExists)
            {
                return BadRequest(new { message = $"Invalid Role ID: {request.RoleId} not found" });
            }
        }
        else
        {
            // Try lookup by name (case-insensitive)
            var role = await _context.Roles.Find(r => r.RoleName.ToLower() == request.RoleId.ToLower()).FirstOrDefaultAsync();
            if (role == null)
            {
                // Try create default roles if not exist (Helper for Demo)
                if (request.RoleId.ToLower() == "admin" || request.RoleId.ToLower() == "manager" || request.RoleId.ToLower() == "hr" || request.RoleId.ToLower() == "employee")
                {
                     var newRole = new Role { RoleName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(request.RoleId) };
                     await _context.Roles.InsertOneAsync(newRole);
                     roleId = newRole.Id!;
                     _logger.LogInformation("✨ Created new role: {RoleName}", newRole.RoleName);
                }
                else
                {
                    return BadRequest(new { message = $"Role '{request.RoleId}' not found. Please use a valid Role ID or Name (Admin, Manager, HR, Employee)." });
                }
            }
            else
            {
                roleId = role.Id!;
            }
        }

        // Hash password
        var hashedPassword = PasswordHelper.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Password = hashedPassword,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.InsertOneAsync(user);

        _logger.LogInformation("✅ Registration successful for user: {Username}", request.Username);

        return Ok(new RegisterResponse
        {
            UserId = user.Id!,
            Username = user.Username,
            Message = "User registered successfully"
        });
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _context.Users.Find(u => u.Id == request.UserId).FirstOrDefaultAsync();
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Verify old password
        if (!PasswordHelper.VerifyPassword(request.OldPassword, user.Password))
        {
            _logger.LogWarning("❌ Password change failed - Invalid old password for user: {UserId}", request.UserId);
            return BadRequest(new { message = "Invalid old password" });
        }

        // Update password
        var hashedPassword = PasswordHelper.HashPassword(request.NewPassword);
        var update = Builders<User>.Update
            .Set(u => u.Password, hashedPassword)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await _context.Users.UpdateOneAsync(u => u.Id == request.UserId, update);

        _logger.LogInformation("✅ Password changed for user: {UserId}", request.UserId);

        return Ok(new { message = "Password changed successfully" });
    }
}

// DTOs
public class LoginRequest
{
    [Required]
    public string Username { get; set; } = null!;
    
    [Required]
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string RoleId { get; set; } = null!;
    public string? RoleName { get; set; }
}

public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = null!;
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = null!;
    
    [Required]
    public string RoleId { get; set; } = null!;
}

public class RegisterResponse
{
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class ChangePasswordRequest
{
    [Required]
    public string UserId { get; set; } = null!;
    
    [Required]
    public string OldPassword { get; set; } = null!;
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = null!;
}
