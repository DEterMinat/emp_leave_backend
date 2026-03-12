using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmployeeLeaveApi.Models;

/// <summary>
/// UserNotification collection according to ER design
/// </summary>
public class UserNotification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    [BsonElement("title")]
    public string Title { get; set; } = null!;

    [BsonElement("message")]
    public string Message { get; set; } = null!;

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("type")]
    public string Type { get; set; } = "Info"; // Info, Leave, System

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
