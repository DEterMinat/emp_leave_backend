using MongoDB.Driver;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Data;

public interface IMongoDbContext
{
    IMongoCollection<User> Users { get; }
    IMongoCollection<Role> Roles { get; }
    IMongoCollection<Department> Departments { get; }
    IMongoCollection<Employee> Employees { get; }
    IMongoCollection<LeaveType> LeaveTypes { get; }
    IMongoCollection<LeaveRequest> LeaveRequests { get; }
    IMongoCollection<LeaveBalance> LeaveBalances { get; }
    IMongoCollection<LeaveAttachment> LeaveAttachments { get; }
    IMongoCollection<DeviceToken> DeviceTokens { get; }
    IMongoCollection<ActivityLog> ActivityLogs { get; }
    Task<bool> TestConnectionAsync();
}
