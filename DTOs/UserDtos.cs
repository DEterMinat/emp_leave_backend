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
}

public class UserUpdateDto
{
    [StringLength(50, MinimumLength = 3)]
    public string? Username { get; set; }

    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }

    public string? RoleId { get; set; }
}

public class UserResponseDto
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string RoleId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Populated field
    public string? RoleName { get; set; }
}
