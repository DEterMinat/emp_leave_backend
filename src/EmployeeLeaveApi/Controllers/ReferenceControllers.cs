using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IMongoDbContext _context;
    private readonly IActivityLogService _logService;

    public RolesController(IMongoDbContext context, IActivityLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll()
    {
        var roles = await _context.Roles.Find(_ => true).ToListAsync();
        return Ok(roles.Select(r => new RoleDto { Id = r.Id!, RoleName = r.RoleName }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDto>> GetById(string id)
    {
        var role = await _context.Roles.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (role == null) return NotFound();
        return Ok(new RoleDto { Id = role.Id!, RoleName = role.RoleName });
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] RoleCreateDto dto)
    {
        var role = new Role { RoleName = dto.RoleName };
        await _context.Roles.InsertOneAsync(role);
        
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "CREATE_ROLE", "Role", role.Id!, $"Created role: {role.RoleName}");
        }

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, new RoleDto { Id = role.Id!, RoleName = role.RoleName });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.Roles.DeleteOneAsync(r => r.Id == id);
        if (result.DeletedCount == 0) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "DELETE_ROLE", "Role", id, $"Deleted role ID: {id}");
        }

        return Ok(new { message = "Role deleted" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IMongoDbContext _context;
    private readonly IActivityLogService _logService;

    public DepartmentsController(IMongoDbContext context, IActivityLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll()
    {
        var departments = await _context.Departments.Find(_ => true).ToListAsync();
        return Ok(departments.Select(d => new DepartmentDto { Id = d.Id!, DepartmentName = d.DepartmentName }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(string id)
    {
        var dept = await _context.Departments.Find(d => d.Id == id).FirstOrDefaultAsync();
        if (dept == null) return NotFound();
        return Ok(new DepartmentDto { Id = dept.Id!, DepartmentName = dept.DepartmentName });
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] DepartmentCreateDto dto)
    {
        var dept = new Department { DepartmentName = dto.DepartmentName };
        await _context.Departments.InsertOneAsync(dept);
        
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "CREATE_DEPT", "Department", dept.Id!, $"Created dept: {dept.DepartmentName}");
        }

        return CreatedAtAction(nameof(GetById), new { id = dept.Id }, new DepartmentDto { Id = dept.Id!, DepartmentName = dept.DepartmentName });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DepartmentDto>> Update(string id, [FromBody] DepartmentUpdateDto dto)
    {
        var update = Builders<Department>.Update.Set(d => d.DepartmentName, dto.DepartmentName);
        var result = await _context.Departments.UpdateOneAsync(d => d.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "UPDATE_DEPT", "Department", id, $"Updated dept to: {dto.DepartmentName}");
        }

        return Ok(new DepartmentDto { Id = id, DepartmentName = dto.DepartmentName });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.Departments.DeleteOneAsync(d => d.Id == id);
        if (result.DeletedCount == 0) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "DELETE_DEPT", "Department", id, $"Deleted dept ID: {id}");
        }

        return Ok(new { message = "Department deleted" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class LeaveTypesController : ControllerBase
{
    private readonly IMongoDbContext _context;
    private readonly IActivityLogService _logService;

    public LeaveTypesController(IMongoDbContext context, IActivityLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaveTypeDto>>> GetAll()
    {
        var types = await _context.LeaveTypes.Find(_ => true).ToListAsync();
        return Ok(types.Select(t => new LeaveTypeDto { Id = t.Id!, TypeName = t.TypeName, Description = t.Description }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveTypeDto>> GetById(string id)
    {
        var type = await _context.LeaveTypes.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (type == null) return NotFound();
        return Ok(new LeaveTypeDto { Id = type.Id!, TypeName = type.TypeName, Description = type.Description });
    }

    [HttpPost]
    public async Task<ActionResult<LeaveTypeDto>> Create([FromBody] LeaveTypeCreateDto dto)
    {
        var type = new LeaveType { TypeName = dto.TypeName, Description = dto.Description };
        await _context.LeaveTypes.InsertOneAsync(type);
        
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "CREATE_LEAVETYPE", "LeaveType", type.Id!, $"Created leave type: {type.TypeName}");
        }

        return CreatedAtAction(nameof(GetById), new { id = type.Id }, new LeaveTypeDto { Id = type.Id!, TypeName = type.TypeName, Description = type.Description });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<LeaveTypeDto>> Update(string id, [FromBody] LeaveTypeUpdateDto dto)
    {
        var update = Builders<LeaveType>.Update
            .Set(t => t.TypeName, dto.TypeName)
            .Set(t => t.Description, dto.Description);
            
        var result = await _context.LeaveTypes.UpdateOneAsync(t => t.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "UPDATE_LEAVETYPE", "LeaveType", id, $"Updated leave type to: {dto.TypeName}");
        }

        return Ok(new LeaveTypeDto { Id = id, TypeName = dto.TypeName, Description = dto.Description });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.LeaveTypes.DeleteOneAsync(t => t.Id == id);
        if (result.DeletedCount == 0) return NotFound();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != null)
        {
            await _logService.LogAsync(currentUserId, "DELETE_LEAVETYPE", "LeaveType", id, $"Deleted leave type ID: {id}");
        }

        return Ok(new { message = "LeaveType deleted" });
    }
}
