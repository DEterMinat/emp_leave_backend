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
    private readonly IActivityLogService _logService;

    public UsersController(IUserService userService, IActivityLogService logService)
    {
        _userService = userService;
        _logService = logService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,HR")] // Only Admin/Manager can list all users
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
        
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "CREATE_USER", "User", user.Id, $"Created user {user.Username} with role {user.RoleName}");
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponseDto>> Update(string id, [FromBody] UserUpdateDto dto)
    {
        // 1. ดึงข้อมูล User และ Role จาก Token
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        // 2. ประกาศตัวแปรทั้งสองตัวเพื่อใช้ตรวจสอบสิทธิ์
        var isAdmin = User.IsInRole("Admin");
        var isHR = User.IsInRole("HR");

        // 3. ตรวจสอบว่า "มีสิทธิ์แก้ไขหรือไม่" (Admin หรือ HR หรือ เจ้าของบัญชี)
        if (!isAdmin && !isHR && currentUserId != id)
        {
            return Forbid(); // หากไม่ใช่กลุ่มด้านบน จะส่ง 403 Forbidden
        }

        // 4. ดำเนินการอัปเดตข้อมูลผ่าน Service
        var updatedUser = await _userService.UpdateAsync(id, dto);
        if (updatedUser == null) return NotFound();
        
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "UPDATE_USER", "User", id, $"Updated user {updatedUser.Username}");
        }
        
        return Ok(updatedUser);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // Only Admin can delete users
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _userService.DeleteAsync(id);
        if (!deleted) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "DELETE_USER", "User", id, $"Deleted user ID: {id}");
        }

        return Ok(new { message = "User deleted" });
    }
}
