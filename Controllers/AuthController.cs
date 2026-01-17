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
    private readonly MongoDbContext _context;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(MongoDbContext context, JwtHelper jwtHelper, ILogger<AuthController> logger)
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

        // Hash password
        var hashedPassword = PasswordHelper.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Password = hashedPassword,
            RoleId = request.RoleId,
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
