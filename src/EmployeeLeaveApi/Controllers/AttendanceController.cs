using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Services;

namespace EmployeeLeaveApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    // POST /api/attendance/check-in
    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceDto>> CheckIn([FromBody] CheckInDto dto)
    {
        try
        {
            var result = await _attendanceService.CheckInAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/attendance/check-out
    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceDto>> CheckOut([FromBody] CheckOutDto dto)
    {
        try
        {
            var result = await _attendanceService.CheckOutAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /api/attendance/today/{employeeId}
    [HttpGet("today/{employeeId}")]
    public async Task<ActionResult<AttendanceDto>> GetTodayAttendance(string employeeId)
    {
        var result = await _attendanceService.GetTodayAttendanceAsync(employeeId);
        if (result == null) return NotFound(new { message = "No attendance record found for today." });
        
        return Ok(result);
    }

    // GET /api/attendance/history/{employeeId}
    [HttpGet("history/{employeeId}")]
    public async Task<ActionResult<List<AttendanceDto>>> GetHistory(string employeeId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var result = await _attendanceService.GetHistoryByEmployeeIdAsync(employeeId, startDate, endDate);
        return Ok(result);
    }

    // GET /api/attendance/all
    [HttpGet("all")]
    [Authorize(Roles = "hr,manager,admin")]
    public async Task<ActionResult<List<AttendanceDto>>> GetAll([FromQuery] DateTime? date)
    {
        var result = await _attendanceService.GetAllAttendanceAsync(date);
        return Ok(result);
    }
}
