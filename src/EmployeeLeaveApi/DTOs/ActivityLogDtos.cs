namespace EmployeeLeaveApi.DTOs;

public class ActivityLogDto
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? Username { get; set; }
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
