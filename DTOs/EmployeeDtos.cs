using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveApi.DTOs;

// ==================== Employee DTOs ====================
public class EmployeeDto
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string DepartmentId { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Populated fields
    public string? DepartmentName { get; set; }
    public string? Username { get; set; }
}

public class EmployeeCreateDto
{
    [Required]
    public string UserId { get; set; } = null!;
    
    [Required]
    public string DepartmentId { get; set; } = null!;
    
    [Required]
    public string FirstName { get; set; } = null!;
    
    [Required]
    public string LastName { get; set; } = null!;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class EmployeeUpdateDto
{
    public string? DepartmentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
