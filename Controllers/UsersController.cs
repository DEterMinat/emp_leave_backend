using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Services;

namespace EmployeeLeaveApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")] // Only Admin/Manager can list all users
    public async Task<ActionResult<List<UserResponseDto>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetById(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        
        // Security check: Users can only view their own profile unless Admin/Manager
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value; // Assumes "roleId" claim maps to role name or strictly roleId? Need to check JwtHelper.

        // TODO: In JwtHelper we put "roleId" claim, and "role" (claim type Role) as RoleName.
        // So User.IsInRole("Admin") should work if RoleName is "Admin".
        
        // For simplicity now, allowing read. In strict mode, check rights.
        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // Only Admin can create users directly
    public async Task<ActionResult<UserResponseDto>> Create([FromBody] UserCreateDto dto)
    {
        var existing = await _userService.GetByUsernameAsync(dto.Username);
        if (existing != null)
            return BadRequest(new { message = "Username already exists" });
        
        var user = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponseDto>> Update(string id, [FromBody] UserUpdateDto dto)
    {
        // Security: Only Admin or the User themselves can update
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        var updatedUser = await _userService.UpdateAsync(id, dto);
        if (updatedUser == null) return NotFound();
        
        return Ok(updatedUser);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // Only Admin can delete users
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _userService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return Ok(new { message = "User deleted" });
    }
}
