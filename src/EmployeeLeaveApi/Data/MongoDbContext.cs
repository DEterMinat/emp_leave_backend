using MongoDB.Driver;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Data;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") 
            ?? configuration["MongoDB:ConnectionString"];
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "emp-leave";

        Console.WriteLine($"📦 Connecting to MongoDB database: {databaseName}");
        
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        
        _client = new MongoClient(settings);
        _database = _client.GetDatabase(databaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Role> Roles => _database.GetCollection<Role>("roles");
    public IMongoCollection<Department> Departments => _database.GetCollection<Department>("departments");
    public IMongoCollection<Employee> Employees => _database.GetCollection<Employee>("employees");
    public IMongoCollection<LeaveType> LeaveTypes => _database.GetCollection<LeaveType>("leaveTypes");
    public IMongoCollection<LeaveRequest> LeaveRequests => _database.GetCollection<LeaveRequest>("leaveRequests");
    public IMongoCollection<LeaveBalance> LeaveBalances => _database.GetCollection<LeaveBalance>("leaveBalances");
    public IMongoCollection<LeaveAttachment> LeaveAttachments => _database.GetCollection<LeaveAttachment>("leaveAttachments");
    public IMongoCollection<DeviceToken> DeviceTokens => _database.GetCollection<DeviceToken>("deviceTokens");


    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
            Console.WriteLine("✅ MongoDB connection successful!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MongoDB connection failed: {ex.Message}");
            return false;
        }
    }
}
