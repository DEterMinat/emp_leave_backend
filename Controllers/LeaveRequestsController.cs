using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Services;

namespace EmployeeLeaveApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveRequestsController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,HR")] // Restricted access
    public async Task<ActionResult<List<LeaveRequestDto>>> GetAll([FromQuery] string? status = null)
    {
        var requests = await _leaveService.GetAllAsync(status);
        return Ok(requests);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(string id)
    {
        var request = await _leaveService.GetByIdAsync(id);
        if (request == null) return NotFound();

        // Security: Employee can only view own requests. Manager/Admin can view all.
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        // This requires logic to check if current user is the owner OR is manager/admin
        // For now, allowing all authorized users to call this, but ideally should be restricted in Service or here.
        
        return Ok(request);
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<List<LeaveRequestDto>>> GetByEmployee(string employeeId)
    {
        // Security: Can only view own if User, or any if Manager
        // Simplified for now
        var requests = await _leaveService.GetByEmployeeIdAsync(employeeId);
        return Ok(requests);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Create([FromBody] LeaveRequestCreateDto dto)
    {
        // Validation should happen in FluentValidation middleware mostly
        var request = await _leaveService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin,Manager,HR")] // Only Approvers
    public async Task<ActionResult<LeaveRequestDto>> Approve(string id, [FromBody] LeaveRequestUpdateDto dto)
    {
        var request = await _leaveService.ApproveAsync(id, dto);
        if (request == null) return NotFound();
        return Ok(request);
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,Manager,HR")] // Only Approvers
    public async Task<ActionResult<LeaveRequestDto>> Reject(string id, [FromBody] LeaveRequestUpdateDto dto)
    {
        var request = await _leaveService.RejectAsync(id, dto);
        if (request == null) return NotFound();
        return Ok(request);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // Only Admin deletes
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _leaveService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return Ok(new { message = "LeaveRequest deleted" });
    }
}
