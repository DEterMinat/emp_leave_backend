using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmployeeLeaveApi.Models;

/// <summary>
/// Attendance collection
/// </summary>
public class Attendance
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AttendanceID { get; set; }

    [BsonElement("employeeId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string EmployeeID { get; set; } = null!;

    [BsonElement("attendanceDate")]
    public DateTime AttendanceDate { get; set; }

    [BsonElement("checkInTime")]
    public DateTime? CheckInTime { get; set; }

    [BsonElement("checkOutTime")]
    public DateTime? CheckOutTime { get; set; }

    [BsonElement("status")]
    public string? Status { get; set; } // e.g., Present, Late, Absent, Leave

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
