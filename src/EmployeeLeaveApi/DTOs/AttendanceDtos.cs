using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveApi.DTOs;

public class AttendanceDto
{
    public string AttendanceID { get; set; } = null!;
    public string EmployeeID { get; set; } = null!;
    public DateTime AttendanceDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CheckInDto
{
    [Required]
    public string EmployeeID { get; set; } = null!;
    public string? Notes { get; set; }
}

public class CheckOutDto
{
    [Required]
    public string EmployeeID { get; set; } = null!;
    public string? Notes { get; set; }
}
