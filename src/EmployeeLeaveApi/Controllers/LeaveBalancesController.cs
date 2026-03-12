using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeaveBalancesController : ControllerBase
{
    private readonly IMongoDbContext _context;

    public LeaveBalancesController(IMongoDbContext context) => _context = context;

    [HttpGet]
    [Authorize(Roles = "admin,manager,hr")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetAll()
    {
        var balances = await _context.LeaveBalances.Find(_ => true).ToListAsync();
        var dtos = new List<LeaveBalanceDto>();

        foreach (var b in balances)
        {
            var type = await _context.LeaveTypes.Find(t => t.Id == b.LeaveTypeId).FirstOrDefaultAsync();
            dtos.Add(MapToDto(b, type));
        }

        return Ok(dtos);
    }

    [HttpGet("employee/{employeeId}")]
    [Authorize(Roles = "admin,manager,hr")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetByEmployee(string employeeId, [FromQuery] int? year = null)
    {
        // Flexible ID lookup: The employeeId passed could be the User.Id (current storage pattern)
        // or the Employee.Id (ER diagram pattern). We should support both.
        
        var targetEmployeeId = employeeId;
        
        // 1. Try to find by direct ID match first
        var balances = await GetBalancesInternal(targetEmployeeId, year);
        
        // 2. If no balances found, it might be that employeeId is the _id of an Employee record
        if (balances.Count == 0)
        {
            var empRecord = await _context.Employees.Find(e => e.Id == employeeId).FirstOrDefaultAsync();
            if (empRecord != null)
            {
                // Try searching using the UserId linked to this employee record
                balances = await GetBalancesInternal(empRecord.UserId, year);
            }
        }

        var dtos = new List<LeaveBalanceDto>();
        foreach (var b in balances)
        {
            var type = await _context.LeaveTypes.Find(t => t.Id == b.LeaveTypeId).FirstOrDefaultAsync();
            dtos.Add(MapToDto(b, type));
        }

        return Ok(dtos);
    }

    private async Task<List<LeaveBalance>> GetBalancesInternal(string empId, int? year)
    {
        var filter = year.HasValue
            ? Builders<LeaveBalance>.Filter.And(
                Builders<LeaveBalance>.Filter.Eq(b => b.EmployeeId, empId),
                Builders<LeaveBalance>.Filter.Eq(b => b.Year, year.Value))
            : Builders<LeaveBalance>.Filter.Eq(b => b.EmployeeId, empId);

        return await _context.LeaveBalances.Find(filter).ToListAsync();
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetMine()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Need to find EmployeeId for this User
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return NotFound("User not found");

        var balances = await _context.LeaveBalances.Find(b => b.EmployeeId == user.Id).ToListAsync();
        var dtos = new List<LeaveBalanceDto>();

        foreach (var b in balances)
        {
            var type = await _context.LeaveTypes.Find(t => t.Id == b.LeaveTypeId).FirstOrDefaultAsync();
            dtos.Add(MapToDto(b, type));
        }

        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<LeaveBalanceDto>> Create([FromBody] LeaveBalanceCreateDto dto)
    {
        var balance = new LeaveBalance
        {
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId,
            Year = dto.Year,
            TotalDays = dto.TotalDays,
            UsedDays = 0,
            RemainingDays = dto.TotalDays
        };

        await _context.LeaveBalances.InsertOneAsync(balance);
        return Ok(MapToDto(balance, null));
    }

    [HttpPost("initialize/{employeeId}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> InitializeBalances(string employeeId, [FromQuery] int year)
    {
        // Resolve ID: use the provided ID if it's already a User, 
        // otherwise look up the UserId if it's an Employee record ID.
        var targetUserId = employeeId;
        var empRecord = await _context.Employees.Find(e => e.Id == employeeId).FirstOrDefaultAsync();
        if (empRecord != null)
        {
            targetUserId = empRecord.UserId;
        }

        // Get all leave types and create default balances
        var leaveTypes = await _context.LeaveTypes.Find(_ => true).ToListAsync();
        var balances = new List<LeaveBalance>();

        var defaultDays = new Dictionary<string, int>
        {
            { "Annual Leave", 6 },
            { "Sick Leave", 30 },
            { "Personal Leave", 3 },
            { "Ordination Leave", 15 }
        };

        foreach (var type in leaveTypes)
        {
            // Check if balance already exists
            var existing = await _context.LeaveBalances.Find(
                b => b.EmployeeId == targetUserId && b.LeaveTypeId == type.Id && b.Year == year
            ).FirstOrDefaultAsync();

            if (existing == null)
            {
                var totalDays = defaultDays.GetValueOrDefault(type.TypeName, 10);
                var balance = new LeaveBalance
                {
                    EmployeeId = targetUserId,
                    LeaveTypeId = type.Id!,
                    Year = year,
                    TotalDays = totalDays,
                    UsedDays = 0,
                    RemainingDays = totalDays
                };
                await _context.LeaveBalances.InsertOneAsync(balance);
                balances.Add(balance);
            }
        }

        return Ok(balances.Select(b => MapToDto(b, null)));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,hr")]
    public async Task<ActionResult> Delete(string id)
    {
        var result = await _context.LeaveBalances.DeleteOneAsync(b => b.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "LeaveBalance deleted" });
    }

    private static LeaveBalanceDto MapToDto(LeaveBalance b, LeaveType? type) => new()
    {
        Id = b.Id!,
        EmployeeId = b.EmployeeId,
        LeaveTypeId = b.LeaveTypeId,
        Year = b.Year,
        TotalDays = b.TotalDays,
        UsedDays = b.UsedDays,
        RemainingDays = b.RemainingDays,
        LeaveTypeName = type?.TypeName
    };
}
