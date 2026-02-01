using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmployeeLeaveApi.Models;

public class ActivityLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = null!;

    [BsonElement("action")]
    public string Action { get; set; } = null!; // e.g., "CREATE_USER", "UPDATE_DEPARTMENT", "APPROVE_LEAVE"

    [BsonElement("targetType")]
    public string TargetType { get; set; } = null!; // e.g., "User", "LeaveRequest", "Department"

    [BsonElement("targetId")]
    public string TargetId { get; set; } = null!;

    [BsonElement("details")]
    public string? Details { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
