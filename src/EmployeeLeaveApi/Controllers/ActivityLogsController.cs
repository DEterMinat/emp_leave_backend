using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeLeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin,hr")]
public class ActivityLogsController : ControllerBase
{
    private readonly IActivityLogService _logService;

    public ActivityLogsController(IActivityLogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ActivityLogDto>>> GetAll([FromQuery] int limit = 100)
    {
        var logs = await _logService.GetAllAsync(limit);
        return Ok(logs);
    }
}
