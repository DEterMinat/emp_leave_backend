using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveApi.DTOs;

public class UserCreateDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = null!;

    [Required]
    public string RoleId { get; set; } = null!;

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? AnnualLeaveQuota { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DepartmentId { get; set; }
}

public class UserUpdateDto
{
    // 1. ปรับ MinimumLength ให้เหลือ 2 (หรือเอาออก) เพื่อให้ใช้ชื่อ "hr" ได้
    [StringLength(50, MinimumLength = 2)] 
    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? RoleId { get; set; }

    // 2. เพิ่มฟิลด์เหล่านี้เข้าไปเพื่อให้ Backend ยอมรับข้อมูลจาก Frontend
    public string? Email { get; set; }
    
    public string? Phone { get; set; }
    
    public int? AnnualLeaveQuota { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DepartmentId { get; set; }
}

public class UserResponseDto
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string RoleId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RoleName { get; set; }

    // --- เพิ่ม 3 บรรทัดนี้เข้าไปครับ เพื่อแก้ Error CS0117 ---
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? AnnualLeaveQuota { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    // -----------------------------------------------------
}
