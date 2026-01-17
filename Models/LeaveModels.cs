using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmployeeLeaveApi.Models;

/// <summary>
/// LeaveRequest collection
/// </summary>
public class LeaveRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("employeeId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EmployeeId { get; set; } = null!;

    [BsonElement("leaveTypeId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string LeaveTypeId { get; set; } = null!;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime EndDate { get; set; }

    [BsonElement("totalDays")]
    public int TotalDays { get; set; }

    [BsonElement("reason")]
    public string Reason { get; set; } = null!;

    [BsonElement("status")]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [BsonElement("comment")]
    public string? Comment { get; set; }

    [BsonElement("requestedDate")]
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

    [BsonElement("approverId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ApproverId { get; set; }

    [BsonElement("approvedDate")]
    public DateTime? ApprovedDate { get; set; }
}

/// <summary>
/// LeaveAttachment collection
/// </summary>
public class LeaveAttachment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("requestId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestId { get; set; } = null!;

    [BsonElement("fileName")]
    public string FileName { get; set; } = null!;

    [BsonElement("filePath")]
    public string FilePath { get; set; } = null!;

    [BsonElement("uploadedDate")]
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// LeaveBalance collection
/// </summary>
public class LeaveBalance
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("employeeId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EmployeeId { get; set; } = null!;

    [BsonElement("leaveTypeId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string LeaveTypeId { get; set; } = null!;

    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("totalDays")]
    public int TotalDays { get; set; }

    [BsonElement("usedDays")]
    public int UsedDays { get; set; }

    [BsonElement("remainingDays")]
    public int RemainingDays { get; set; }
}
