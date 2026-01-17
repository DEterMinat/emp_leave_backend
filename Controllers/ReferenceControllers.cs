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
    private readonly MongoDbContext _context;

    public RolesController(MongoDbContext context) => _context = context;

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
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, new RoleDto { Id = role.Id!, RoleName = role.RoleName });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.Roles.DeleteOneAsync(r => r.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "Role deleted" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly MongoDbContext _context;

    public DepartmentsController(MongoDbContext context) => _context = context;

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
        return CreatedAtAction(nameof(GetById), new { id = dept.Id }, new DepartmentDto { Id = dept.Id!, DepartmentName = dept.DepartmentName });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.Departments.DeleteOneAsync(d => d.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "Department deleted" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class LeaveTypesController : ControllerBase
{
    private readonly MongoDbContext _context;

    public LeaveTypesController(MongoDbContext context) => _context = context;

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
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, new LeaveTypeDto { Id = type.Id!, TypeName = type.TypeName, Description = type.Description });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.LeaveTypes.DeleteOneAsync(t => t.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "LeaveType deleted" });
    }
}
