using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveApi.DTOs;

// ==================== Role DTOs ====================
public class RoleDto
{
    public string Id { get; set; } = null!;
    public string RoleName { get; set; } = null!;
}

public class RoleCreateDto
{
    [Required]
    public string RoleName { get; set; } = null!;
}

// ==================== Department DTOs ====================
public class DepartmentDto
{
    public string Id { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
}

public class DepartmentCreateDto
{
    [Required]
    public string DepartmentName { get; set; } = null!;
}

// ==================== LeaveType DTOs ====================
public class LeaveTypeDto
{
    public string Id { get; set; } = null!;
    public string TypeName { get; set; } = null!;
    public string? Description { get; set; }
}

public class LeaveTypeCreateDto
{
    [Required]
    public string TypeName { get; set; } = null!;
    public string? Description { get; set; }
}
