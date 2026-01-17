using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmployeeLeaveApi.Models;

/// <summary>
/// Role collection
/// </summary>
public class Role
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("roleName")]
    public string RoleName { get; set; } = null!;
}

/// <summary>
/// Department collection
/// </summary>
public class Department
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("departmentName")]
    public string DepartmentName { get; set; } = null!;
}

/// <summary>
/// LeaveType collection
/// </summary>
public class LeaveType
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("typeName")]
    public string TypeName { get; set; } = null!;

    [BsonElement("description")]
    public string? Description { get; set; }
}
