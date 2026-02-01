using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Services;

public interface IActivityLogService
{
    Task LogAsync(string userId, string action, string targetType, string targetId, string? details = null);
    Task<List<ActivityLogDto>> GetAllAsync(int limit = 100);
}
