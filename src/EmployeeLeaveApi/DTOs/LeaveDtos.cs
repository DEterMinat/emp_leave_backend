using System.ComponentModel.DataAnnotations;

namespace EmployeeLeaveApi.DTOs;

// ==================== LeaveRequest DTOs ====================
public class LeaveRequestDto
{
    public string Id { get; set; } = null!;
    public string EmployeeId { get; set; } = null!;
    public string LeaveTypeId { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Comment { get; set; }
    public DateTime RequestedDate { get; set; }
    public string? ApproverId { get; set; }
    public DateTime? ApprovedDate { get; set; }

    // Populated fields
    public string? EmployeeName { get; set; }
    public string? LeaveTypeName { get; set; }
    public string? ApproverName { get; set; }
    public bool HasAttachments { get; set; }
}

public class LeaveRequestCreateDto
{
    [Required]
    public string EmployeeId { get; set; } = null!;

    [Required]
    public string LeaveTypeId { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public string Reason { get; set; } = null!;
}

public class LeaveRequestCreateWithFileDto : LeaveRequestCreateDto
{
    public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
}

public class LeaveRequestUpdateDto
{
    public string? Status { get; set; } // Approved, Rejected
    public string? Comment { get; set; }
    public string? ApproverId { get; set; }
}

// ==================== LeaveBalance DTOs ====================
public class LeaveBalanceDto
{
    public string Id { get; set; } = null!;
    public string EmployeeId { get; set; } = null!;
    public string LeaveTypeId { get; set; } = null!;
    public int Year { get; set; }
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays { get; set; }

    // Populated fields
    public string? LeaveTypeName { get; set; }
}

public class LeaveBalanceCreateDto
{
    [Required]
    public string EmployeeId { get; set; } = null!;

    [Required]
    public string LeaveTypeId { get; set; } = null!;

    [Required]
    public int Year { get; set; }

    [Required]
    public int TotalDays { get; set; }
}

// ==================== LeaveAttachment DTOs ====================
public class LeaveAttachmentDto
{
    public string Id { get; set; } = null!;
    public string RequestId { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateTime UploadedDate { get; set; }
}

public class LeaveAttachmentCreateDto
{
    [Required]
    public string RequestId { get; set; } = null!;

    [Required]
    public string FileName { get; set; } = null!;

    [Required]
    public string FilePath { get; set; } = null!;
}
