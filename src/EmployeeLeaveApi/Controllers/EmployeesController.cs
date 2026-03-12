using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Controllers;

[Authorize(Roles = "admin,manager,hr")]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IMongoDbContext _context;

    public EmployeesController(IMongoDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll()
    {
        var employees = await _context.Employees.Find(_ => true).ToListAsync();
        var dtos = new List<EmployeeDto>();

        foreach (var e in employees)
        {
            var dept = await _context.Departments.Find(d => d.Id == e.DepartmentId).FirstOrDefaultAsync();
            var user = await _context.Users.Find(u => u.Id == e.UserId).FirstOrDefaultAsync();

            dtos.Add(new EmployeeDto
            {
                Id = e.Id!,
                UserId = e.UserId,
                DepartmentId = e.DepartmentId,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                Address = e.Address,
                Position = e.Position,
                Salary = e.Salary,
                CreatedAt = e.CreatedAt,
                DepartmentName = dept?.DepartmentName,
                Username = user?.Username
            });
        }

        return Ok(dtos);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<EmployeeDto>> GetByUserId(string userId)
    {
        var e = await _context.Employees.Find(emp => emp.UserId == userId).FirstOrDefaultAsync();
        if (e == null) return NotFound();

        var dept = await _context.Departments.Find(d => d.Id == e.DepartmentId).FirstOrDefaultAsync();
        var user = await _context.Users.Find(u => u.Id == e.UserId).FirstOrDefaultAsync();

        return Ok(new EmployeeDto
        {
            Id = e.Id!,
            UserId = e.UserId,
            DepartmentId = e.DepartmentId,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            Address = e.Address,
            Position = e.Position,
            Salary = e.Salary,
            CreatedAt = e.CreatedAt,
            DepartmentName = dept?.DepartmentName,
            Username = user?.Username
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetById(string id)
    {
        var e = await _context.Employees.Find(emp => emp.Id == id).FirstOrDefaultAsync();
        if (e == null) return NotFound();

        var dept = await _context.Departments.Find(d => d.Id == e.DepartmentId).FirstOrDefaultAsync();
        var user = await _context.Users.Find(u => u.Id == e.UserId).FirstOrDefaultAsync();

        return Ok(new EmployeeDto
        {
            Id = e.Id!,
            UserId = e.UserId,
            DepartmentId = e.DepartmentId,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            Address = e.Address,
            Position = e.Position,
            Salary = e.Salary,
            CreatedAt = e.CreatedAt,
            DepartmentName = dept?.DepartmentName,
            Username = user?.Username
        });
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] EmployeeCreateDto dto)
    {
        var employee = new Employee
        {
            UserId = dto.UserId,
            DepartmentId = dto.DepartmentId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            Position = dto.Position,
            Salary = dto.Salary,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Employees.InsertOneAsync(employee);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id },
            new EmployeeDto
            {
                Id = employee.Id!,
                UserId = employee.UserId,
                DepartmentId = employee.DepartmentId,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                Address = employee.Address,
                Position = employee.Position,
                Salary = employee.Salary,
                CreatedAt = employee.CreatedAt
            });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeDto>> Update(string id, [FromBody] EmployeeUpdateDto dto)
    {
        var update = Builders<Employee>.Update.Set(e => e.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(dto.DepartmentId)) update = update.Set(e => e.DepartmentId, dto.DepartmentId);
        if (!string.IsNullOrEmpty(dto.FirstName)) update = update.Set(e => e.FirstName, dto.FirstName);
        if (!string.IsNullOrEmpty(dto.LastName)) update = update.Set(e => e.LastName, dto.LastName);
        if (!string.IsNullOrEmpty(dto.Email)) update = update.Set(e => e.Email, dto.Email);
        if (dto.Phone != null) update = update.Set(e => e.Phone, dto.Phone);
        if (dto.Address != null) update = update.Set(e => e.Address, dto.Address);
        if (dto.Position != null) update = update.Set(e => e.Position, dto.Position);
        if (dto.Salary.HasValue) update = update.Set(e => e.Salary, dto.Salary.Value);

        var result = await _context.Employees.UpdateOneAsync(e => e.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();

        return await GetById(id);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.Employees.DeleteOneAsync(e => e.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "Employee deleted" });
    }
}
