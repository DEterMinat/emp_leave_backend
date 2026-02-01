using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveBalancesController : ControllerBase
{
    private readonly IMongoDbContext _context;

    public LeaveBalancesController(IMongoDbContext context) => _context = context;

    [HttpGet]
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
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetByEmployee(string employeeId, [FromQuery] int? year = null)
    {
        var filter = year.HasValue
            ? Builders<LeaveBalance>.Filter.And(
                Builders<LeaveBalance>.Filter.Eq(b => b.EmployeeId, employeeId),
                Builders<LeaveBalance>.Filter.Eq(b => b.Year, year.Value))
            : Builders<LeaveBalance>.Filter.Eq(b => b.EmployeeId, employeeId);

        var balances = await _context.LeaveBalances.Find(filter).ToListAsync();
        var dtos = new List<LeaveBalanceDto>();

        foreach (var b in balances)
        {
            var type = await _context.LeaveTypes.Find(t => t.Id == b.LeaveTypeId).FirstOrDefaultAsync();
            dtos.Add(MapToDto(b, type));
        }

        return Ok(dtos);
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
    public async Task<ActionResult<List<LeaveBalanceDto>>> InitializeBalances(string employeeId, [FromQuery] int year)
    {
        // Get all leave types and create default balances
        var leaveTypes = await _context.LeaveTypes.Find(_ => true).ToListAsync();
        var balances = new List<LeaveBalance>();

        var defaultDays = new Dictionary<string, int>
        {
            { "ลาพักผ่อน", 6 },
            { "Annual Leave", 6 },
            { "ลาป่วย", 30 },
            { "Sick Leave", 30 },
            { "ลากิจ", 3 },
            { "Personal Leave", 3 },
            { "ลาอุปสมบท", 15 },
            { "Ordination Leave", 15 }
        };

        foreach (var type in leaveTypes)
        {
            // Check if balance already exists
            var existing = await _context.LeaveBalances.Find(
                b => b.EmployeeId == employeeId && b.LeaveTypeId == type.Id && b.Year == year
            ).FirstOrDefaultAsync();

            if (existing == null)
            {
                var totalDays = defaultDays.GetValueOrDefault(type.TypeName, 10);
                var balance = new LeaveBalance
                {
                    EmployeeId = employeeId,
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
