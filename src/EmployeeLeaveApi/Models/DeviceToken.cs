using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmployeeLeaveApi.Models;

public class DeviceToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Android"; // Android, iOS
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
