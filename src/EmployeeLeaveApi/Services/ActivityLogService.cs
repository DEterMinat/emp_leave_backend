using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;
using MongoDB.Driver;

namespace EmployeeLeaveApi.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IMongoDbContext _context;

    public ActivityLogService(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string userId, string action, string targetType, string targetId, string? details = null)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ActivityLogs.InsertOneAsync(log);
    }

    public async Task<List<ActivityLogDto>> GetAllAsync(int limit = 100)
    {
        var logs = await _context.ActivityLogs
            .Find(_ => true)
            .SortByDescending(l => l.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        var dtos = new List<ActivityLogDto>();
        foreach (var log in logs)
        {
            var user = await _context.Users.Find(u => u.Id == log.UserId).FirstOrDefaultAsync();
            dtos.Add(new ActivityLogDto
            {
                Id = log.Id!,
                UserId = log.UserId,
                Username = user?.Username ?? "Unknown",
                Action = log.Action,
                TargetType = log.TargetType,
                TargetId = log.TargetId,
                Details = log.Details,
                CreatedAt = log.CreatedAt
            });
        }
        return dtos;
    }
}
