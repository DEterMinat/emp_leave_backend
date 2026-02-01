using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmployeeLeaveApi.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("username")]
    public string Username { get; set; } = null!;

    [BsonElement("password")]
    public string Password { get; set; } = null!;

    [BsonElement("roleId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RoleId { get; set; } = null!;

    // --- เพิ่มฟิลด์ใหม่เหล่านี้เข้าไปเพื่อให้ MongoDB มีที่เก็บข้อมูลครับ ---
    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("annualLeaveQuota")]
    public int? AnnualLeaveQuota { get; set; }

    [BsonElement("firstName")]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    public string? LastName { get; set; }

    [BsonElement("departmentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? DepartmentId { get; set; }
    // -----------------------------------------------------------

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}