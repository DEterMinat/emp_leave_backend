using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeviceTokensController : ControllerBase
{
    private readonly IMongoDbContext _context;

    public DeviceTokensController(IMongoDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] DeviceTokenRegisterDto dto)
    {
        var filter = Builders<DeviceToken>.Filter.And(
            Builders<DeviceToken>.Filter.Eq(t => t.UserId, dto.UserId),
            Builders<DeviceToken>.Filter.Eq(t => t.Token, dto.Token)
        );

        var existing = await _context.DeviceTokens.Find(filter).FirstOrDefaultAsync();

        if (existing == null)
        {
            var deviceToken = new DeviceToken
            {
                UserId = dto.UserId,
                Token = dto.Token,
                DeviceType = dto.DeviceType,
                LastUpdated = DateTime.UtcNow
            };
            await _context.DeviceTokens.InsertOneAsync(deviceToken);
        }
        else
        {
            var update = Builders<DeviceToken>.Update.Set(t => t.LastUpdated, DateTime.UtcNow);
            await _context.DeviceTokens.UpdateOneAsync(filter, update);
        }

        return Ok(new { message = "Token registered successfully" });
    }
}

public class DeviceTokenRegisterDto
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Android";
}
