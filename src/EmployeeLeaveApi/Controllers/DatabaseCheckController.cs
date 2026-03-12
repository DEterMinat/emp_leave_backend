using Microsoft.AspNetCore.Mvc;
using EmployeeLeaveApi.Data;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;

namespace EmployeeLeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseCheckController : ControllerBase
{
    private readonly IMongoDbContext _context;
    private readonly ILogger<DatabaseCheckController> _logger;

    public DatabaseCheckController(IMongoDbContext context, ILogger<DatabaseCheckController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("inspect")]
    public async Task<IActionResult> Inspect()
    {
        try
        {
            var database = _context.Users.Database;
            var collections = await database.ListCollectionNames().ToListAsync();
            var counts = new Dictionary<string, long>();

            foreach (var name in collections)
            {
                var count = await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(new BsonDocument());
                counts[name] = count;
            }

            // Sample some users to see IDs
            var users = await database.GetCollection<BsonDocument>("users").Find(new BsonDocument()).Limit(10).ToListAsync();
            var userDetails = users.Select(u => new {
                Id = u["_id"].ToString(),
                Type = u["_id"].BsonType.ToString(),
                Username = u.Contains("username") ? u["username"].ToString() : "N/A"
            }).ToList();

            return Ok(new {
                Collections = counts,
                Users = userDetails
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
    [HttpGet("status")]
    public async Task<IActionResult> CheckDatabaseStatus()
    {
        try
        {
            var results = new List<object>();

            // 1. Users
            var usersCount = await _context.Users.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "users",
                Count = usersCount,
                Status = usersCount > 0 ? "✅ Has Data" : "⚠️ Empty",
                Required = true
            });

            // 2. Roles
            var rolesCount = await _context.Roles.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "roles",
                Count = rolesCount,
                Status = rolesCount > 0 ? "✅ Has Data" : "⚠️ Empty",
                Required = true
            });

            // 3. Departments
            var deptsCount = await _context.Departments.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "departments",
                Count = deptsCount,
                Status = deptsCount > 0 ? "✅ Has Data" : "⚠️ Empty",
                Required = true
            });

            // 4. Employees
            var empsCount = await _context.Employees.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "employees",
                Count = empsCount,
                Status = empsCount > 0 ? "✅ Has Data" : "⚠️ Empty",
                Required = true
            });

            // 5. LeaveTypes
            var typesCount = await _context.LeaveTypes.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "leaveTypes",
                Count = typesCount,
                Status = typesCount > 0 ? "✅ Has Data" : "⚠️ Empty",
                Required = true
            });

            // 6. LeaveRequests
            var reqsCount = await _context.LeaveRequests.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "leaveRequests",
                Count = reqsCount,
                Status = reqsCount > 0 ? "✅ Has Data" : "ℹ️ Empty (optional)",
                Required = false
            });

            // 7. LeaveBalances
            var balsCount = await _context.LeaveBalances.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "leaveBalances",
                Count = balsCount,
                Status = balsCount > 0 ? "✅ Has Data" : "⚠️ Empty",
                Required = true
            });

            // 8. LeaveAttachments
            var attachCount = await _context.LeaveAttachments.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "leaveAttachments",
                Count = attachCount,
                Status = attachCount > 0 ? "✅ Has Data" : "ℹ️ Empty (optional)",
                Required = false
            });

            // 9. DeviceTokens
            var tokensCount = await _context.DeviceTokens.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "deviceTokens",
                Count = tokensCount,
                Status = tokensCount > 0 ? "✅ Has Data" : "ℹ️ Empty (optional)",
                Required = false
            });

            // 10. ActivityLogs
            var logsCount = await _context.ActivityLogs.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "activityLogs",
                Count = logsCount,
                Status = logsCount > 0 ? "✅ Has Data" : "ℹ️ Empty (optional)",
                Required = false
            });

            // 11. Attendances
            var attendCount = await _context.Attendances.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "attendances",
                Count = attendCount,
                Status = attendCount > 0 ? "✅ Has Data" : "ℹ️ Empty (optional)",
                Required = false
            });

            // 12. UserNotifications
            var notifyCount = await _context.UserNotifications.CountDocumentsAsync(_ => true);
            results.Add(new
            {
                Collection = "userNotifications",
                Count = notifyCount,
                Status = notifyCount > 0 ? "✅ Has Data" : "ℹ️ Empty (optional)",
                Required = false
            });

            var totalCollections = results.Count;
            var withData = results.Count(r => ((dynamic)r).Count > 0);
            var requiredEmpty = results.Where(r => ((dynamic)r).Required && ((dynamic)r).Count == 0).Count();

            return Ok(new
            {
                Summary = new
                {
                    TotalCollections = totalCollections,
                    WithData = withData,
                    Empty = totalCollections - withData,
                    RequiredEmpty = requiredEmpty,
                    OverallStatus = requiredEmpty == 0 ? "✅ All required collections have data" : $"⚠️ {requiredEmpty} required collection(s) are empty"
                },
                Collections = results,
                Recommendations = GenerateRecommendations(results)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = "Failed to check database status",
                Message = ex.Message
            });
        }
    }

    private List<string> GenerateRecommendations(List<object> results)
    {
        var recommendations = new List<string>();

        foreach (var result in results)
        {
            dynamic r = result;
            if (r.Required && r.Count == 0)
            {
                recommendations.Add($"⚠️ Seed {r.Collection} - This is required for the system to work");
            }
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("✅ All required collections are populated!");
        }

        return recommendations;
    }

    /// <summary>
    /// Seed all required master data
    /// </summary>
    [HttpPost("seed-master-data")]
    public async Task<IActionResult> SeedMasterData()
    {
        try
        {
            var messages = new List<string>();

            // 1. Seed Roles
            var rolesCount = await _context.Roles.CountDocumentsAsync(_ => true);
            if (rolesCount == 0)
            {
                var roles = new[]
                {
                    new Models.Role { RoleName = "Admin" },
                    new Models.Role { RoleName = "Manager" },
                    new Models.Role { RoleName = "HR" },
                    new Models.Role { RoleName = "Employee" }
                };
                await _context.Roles.InsertManyAsync(roles);
                messages.Add("✅ Seeded 4 roles (Admin, Manager, HR, Employee)");
            }
            else
            {
                messages.Add($"ℹ️ Roles already exist ({rolesCount} records)");
            }

            // 2. Seed Departments
            var deptsCount = await _context.Departments.CountDocumentsAsync(_ => true);
            if (deptsCount == 0)
            {
                var departments = new[]
                {
                    new Models.Department { DepartmentName = "IT" },
                    new Models.Department { DepartmentName = "HR" },
                    new Models.Department { DepartmentName = "Finance" },
                    new Models.Department { DepartmentName = "Sales" },
                    new Models.Department { DepartmentName = "Marketing" }
                };
                await _context.Departments.InsertManyAsync(departments);
                messages.Add("✅ Seeded 5 departments");
            }
            else
            {
                messages.Add($"ℹ️ Departments already exist ({deptsCount} records)");
            }

            // 3. Seed LeaveTypes
            var typesCount = await _context.LeaveTypes.CountDocumentsAsync(_ => true);
            if (typesCount == 0)
            {
                var leaveTypes = new[]
                {
                    new Models.LeaveType { TypeName = "Annual Leave", Description = "ลาพักร้อนประจำปี" },
                    new Models.LeaveType { TypeName = "Sick Leave", Description = "ลาป่วย" },
                    new Models.LeaveType { TypeName = "Personal Leave", Description = "ลากิจ" },
                    new Models.LeaveType { TypeName = "Ordination Leave", Description = "ลาบวช" }
                };
                await _context.LeaveTypes.InsertManyAsync(leaveTypes);
                messages.Add("✅ Seeded 4 leave types");
            }
            else
            {
                messages.Add($"ℹ️ Leave types already exist ({typesCount} records)");
            }

            // 4. Create sample users with hashed passwords
            var usersCount = await _context.Users.CountDocumentsAsync(_ => true);
            if (usersCount == 0)
            {
                // Get role IDs
                var adminRole = await _context.Roles.Find(r => r.RoleName == "Admin").FirstOrDefaultAsync();
                var managerRole = await _context.Roles.Find(r => r.RoleName == "Manager").FirstOrDefaultAsync();
                var hrRole = await _context.Roles.Find(r => r.RoleName == "HR").FirstOrDefaultAsync();
                var empRole = await _context.Roles.Find(r => r.RoleName == "Employee").FirstOrDefaultAsync();

                if (adminRole != null && empRole != null)
                {
                    var users = new[]
                    {
                        new Models.User 
                        { 
                            Username = "admin",
                            Password = Helpers.PasswordHelper.HashPassword("admin123"),
                            RoleId = adminRole.Id!,
                            Email = "admin@company.com",
                            FirstName = "Admin",
                            LastName = "System",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Models.User 
                        { 
                            Username = "manager01",
                            Password = Helpers.PasswordHelper.HashPassword("manager123"),
                            RoleId = managerRole?.Id ?? empRole.Id!,
                            Email = "manager@company.com",
                            FirstName = "Manager",
                            LastName = "One",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Models.User 
                        { 
                            Username = "hr01",
                            Password = Helpers.PasswordHelper.HashPassword("hr123"),
                            RoleId = hrRole?.Id ?? empRole.Id!,
                            Email = "hr@company.com",
                            FirstName = "HR",
                            LastName = "Staff",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Models.User 
                        { 
                            Username = "emp001",
                            Password = Helpers.PasswordHelper.HashPassword("emp123"),
                            RoleId = empRole.Id!,
                            Email = "employee@company.com",
                            FirstName = "John",
                            LastName = "Doe",
                            CreatedAt = DateTime.UtcNow
                        }
                    };
                    await _context.Users.InsertManyAsync(users);
                    messages.Add("✅ Seeded 4 sample users (admin/admin123, manager01/manager123, hr01/hr123, emp001/emp123)");
                }
            }
            else
            {
                messages.Add($"ℹ️ Users already exist ({usersCount} records)");
            }

            // 5. Create employees for each user
            var empsCount = await _context.Employees.CountDocumentsAsync(_ => true);
            if (empsCount == 0)
            {
                var itDept = await _context.Departments.Find(d => d.DepartmentName == "IT").FirstOrDefaultAsync();
                var users = await _context.Users.Find(_ => true).ToListAsync();

                if (itDept != null && users.Any())
                {
                    var employees = new List<Models.Employee>();
                    foreach (var user in users)
                    {
                        employees.Add(new Models.Employee
                        {
                            UserId = user.Id!,
                            DepartmentId = itDept.Id!,
                            FirstName = user.FirstName ?? "Unknown",
                            LastName = user.LastName ?? "User",
                            Email = user.Email ?? $"{user.Username}@company.com",
                            Phone = "0812345678",
                            Position = "Staff",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.Employees.InsertManyAsync(employees);
                    messages.Add($"✅ Created {employees.Count} employee records linked to users");
                }
            }
            else
            {
                messages.Add($"ℹ️ Employees already exist ({empsCount} records)");
            }

            // 6. Create leave balances for employees
            var balsCount = await _context.LeaveBalances.CountDocumentsAsync(_ => true);
            if (balsCount == 0)
            {
                var employees = await _context.Employees.Find(_ => true).ToListAsync();
                var annualLeaveType = await _context.LeaveTypes.Find(lt => lt.TypeName == "Annual Leave").FirstOrDefaultAsync();
                var sickLeaveType = await _context.LeaveTypes.Find(lt => lt.TypeName == "Sick Leave").FirstOrDefaultAsync();

                if (annualLeaveType != null && sickLeaveType != null && employees.Any())
                {
                    var balances = new List<Models.LeaveBalance>();
                    var currentYear = DateTime.UtcNow.Year;

                    foreach (var emp in employees)
                    {
                        // Annual leave - 10 days
                        balances.Add(new Models.LeaveBalance
                        {
                            EmployeeId = emp.Id!,
                            LeaveTypeId = annualLeaveType.Id!,
                            Year = currentYear,
                            TotalDays = 10,
                            UsedDays = 0,
                            RemainingDays = 10
                        });

                        // Sick leave - 30 days
                        balances.Add(new Models.LeaveBalance
                        {
                            EmployeeId = emp.Id!,
                            LeaveTypeId = sickLeaveType.Id!,
                            Year = currentYear,
                            TotalDays = 30,
                            UsedDays = 0,
                            RemainingDays = 30
                        });
                    }

                    await _context.LeaveBalances.InsertManyAsync(balances);
                    messages.Add($"✅ Created {balances.Count} leave balance records for employees");
                }
            }
            else
            {
                messages.Add($"ℹ️ Leave balances already exist ({balsCount} records)");
            }

            // 7. Create sample attendance records
            var attendCount = await _context.Attendances.CountDocumentsAsync(_ => true);
            if (attendCount == 0)
            {
                var employees = await _context.Employees.Find(_ => true).ToListAsync();
                if (employees.Any())
                {
                    var attendances = new List<Models.Attendance>();
                    var today = DateTime.UtcNow;

                    // Create attendance for last 7 days for each employee
                    foreach (var emp in employees)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            var date = today.AddDays(-i).Date;
                            var checkIn = date.AddHours(8).AddMinutes(new Random().Next(0, 30)); // 8:00-8:30 AM
                            var checkOut = date.AddHours(17).AddMinutes(new Random().Next(0, 30)); // 5:00-5:30 PM

                            attendances.Add(new Models.Attendance
                            {
                                EmployeeID = emp.Id!,
                                AttendanceDate = date,
                                CheckInTime = checkIn,
                                CheckOutTime = checkOut,
                                Status = i == 0 ? "Present" : (i % 5 == 0 ? "Late" : "Present"),
                                Notes = i % 5 == 0 ? "Late arrival" : null,
                                CreatedAt = date
                            });
                        }
                    }

                    await _context.Attendances.InsertManyAsync(attendances);
                    messages.Add($"✅ Created {attendances.Count} sample attendance records (last 7 days for each employee)");
                }
            }
            else
            {
                messages.Add($"ℹ️ Attendances already exist ({attendCount} records)");
            }

            return Ok(new
            {
                Success = true,
                Message = "Master data seeding completed",
                Details = messages
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Error = "Failed to seed master data",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Sync missing employee and leave balance records for existing users
    /// </summary>
    [HttpPost("sync-missing-records")]
    public async Task<IActionResult> SyncMissingRecords()
    {
        _logger.LogInformation("🔄 Starting SyncMissingRecords...");
        try
        {
            var results = new List<string>();
            var users = await _context.Users.Find(_ => true).ToListAsync();
            _logger.LogInformation("Found {Count} users", users.Count);

            var itDept = await _context.Departments.Find(d => d.DepartmentName == "IT").FirstOrDefaultAsync();
            var leaveTypes = await _context.LeaveTypes.Find(_ => true).ToListAsync();
            _logger.LogInformation("Found {Count} leave types", leaveTypes.Count);

            var currentYear = DateTime.UtcNow.Year;

            foreach (var user in users)
            {
                try
                {
                    _logger.LogInformation("Processing user: {Username} ({Id})", user.Username, user.Id);

                    // 1. Sync Employee Record
                    var existingEmp = await _context.Employees.Find(e => e.UserId == user.Id).FirstOrDefaultAsync();
                    if (existingEmp == null)
                    {
                        _logger.LogInformation("Creating missing Employee for {Username}", user.Username);
                        var employee = new Models.Employee
                        {
                            UserId = user.Id!,
                            DepartmentId = !string.IsNullOrEmpty(user.DepartmentId) ? user.DepartmentId : (itDept?.Id ?? ""),
                            FirstName = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName : user.Username,
                            LastName = user.LastName ?? "",
                            Email = !string.IsNullOrEmpty(user.Email) ? user.Email : $"{user.Username}@company.com",
                            Phone = user.Phone,
                            Position = !string.IsNullOrEmpty(user.Position) ? user.Position : "Staff",
                            Salary = user.Salary,
                            CreatedAt = DateTime.UtcNow
                        };

                        // Final safety check: if DepartmentId is still empty, we must NOT insert or it will crash serialization
                        if (string.IsNullOrEmpty(employee.DepartmentId))
                        {
                            _logger.LogWarning("⚠️ Could not find a valid DepartmentId for user {Username}. Skipping employee creation.", user.Username);
                            results.Add($"⚠️ Skipped employee creation for {user.Username} (Missing Department)");
                            continue;
                        }

                        await _context.Employees.InsertOneAsync(employee);
                        results.Add($"✅ Created Employee for user: {user.Username}");
                    }

                    // 2. Sync Leave Balances
                    // Get the actual employee record to use its ID mapping if needed, 
                    // though current services use User.Id as "EmployeeId"
                    var hasBalances = await _context.LeaveBalances.Find(b => b.EmployeeId == user.Id).AnyAsync();
                    _logger.LogInformation("User {Username} has balances: {HasBalances}", user.Username, hasBalances);

                    if (!hasBalances)
                    {
                        _logger.LogInformation("Initializing balances for {Username}", user.Username);
                        int balancesCreated = 0;
                        foreach (var type in leaveTypes)
                        {
                            int totalDays = type.TypeName switch
                            {
                                "Annual Leave" => user.AnnualLeaveQuota ?? 10,
                                "Sick Leave" => 30,
                                "Personal Leave" => 3,
                                "Ordination Leave" => 15,
                                _ => 10
                            };

                            var balance = new Models.LeaveBalance
                            {
                                EmployeeId = user.Id!,
                                LeaveTypeId = type.Id!,
                                Year = currentYear,
                                TotalDays = totalDays,
                                UsedDays = 0,
                                RemainingDays = totalDays
                            };
                            await _context.LeaveBalances.InsertOneAsync(balance);
                            balancesCreated++;
                        }
                        _logger.LogInformation("Initialized {Count} balances for {Username}", balancesCreated, user.Username);
                        results.Add($"✅ Initialized {balancesCreated} leave balances for user: {user.Username}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to sync user {Username}", user.Username);
                    results.Add($"❌ Failed to sync user {user.Username}: {ex.Message}");
                }
            }

            if (!results.Any())
            {
                _logger.LogInformation("Sync completed: Database already in sync.");
                return Ok(new { Message = "Database is already in sync." });
            }

            _logger.LogInformation("Sync completed successfully. {Count} changes made.", results.Count);
            return Ok(new { Message = "Sync completed", Details = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Sync failed");
            return StatusCode(500, new { Error = "Sync failed", Message = ex.Message });
        }
    }

    /// <summary>
    /// Forces a complete system seed, creating missing master data and linking all users.
    /// </summary>
    [HttpPost("force-seed-all")]
    public async Task<IActionResult> ForceSeedAll()
    {
        _logger.LogInformation("🔥 Starting ForceSeedAll...");
        try
        {
            var reports = new List<string>();

            // 1. Ensure Roles
            var roles = new[] { "Admin", "Manager", "HR", "Employee" };
            foreach (var roleName in roles)
            {
                var existing = await _context.Roles.Find(r => r.RoleName == roleName).AnyAsync();
                if (!existing)
                {
                    await _context.Roles.InsertOneAsync(new Models.Role { RoleName = roleName });
                    reports.Add($"✅ Created Role: {roleName}");
                }
            }

            // 2. Ensure Default Department
            var dept = await _context.Departments.Find(d => d.DepartmentName == "General").FirstOrDefaultAsync();
            if (dept == null)
            {
                dept = new Models.Department { DepartmentName = "General" };
                await _context.Departments.InsertOneAsync(dept);
                reports.Add("✅ Created Default Department: General");
            }

            // 3. Ensure Standard Leave Types
            var defaultLeaveTypes = new[]
            {
                new { Name = "Annual Leave", Days = 6 },
                new { Name = "Sick Leave", Days = 30 },
                new { Name = "Personal Leave", Days = 3 },
                new { Name = "Ordination Leave", Days = 15 }
            };

            foreach (var lt in defaultLeaveTypes)
            {
                var existing = await _context.LeaveTypes.Find(t => t.TypeName == lt.Name).FirstOrDefaultAsync();
                if (existing == null)
                {
                    await _context.LeaveTypes.InsertOneAsync(new Models.LeaveType { TypeName = lt.Name, Description = $"Default {lt.Name}" });
                    reports.Add($"✅ Created Leave Type: {lt.Name}");
                }
            }

            // Reload leave types after seeding
            var allLeaveTypes = await _context.LeaveTypes.Find(_ => true).ToListAsync();

            // 4. Process all Users
            var users = await _context.Users.Find(_ => true).ToListAsync();
            var adminRole = await _context.Roles.Find(r => r.RoleName == "Admin").FirstOrDefaultAsync();
            var empRole = await _context.Roles.Find(r => r.RoleName == "Employee").FirstOrDefaultAsync();

            int userFixes = 0;
            int balanceFixes = 0;

            foreach (var user in users)
            {
                bool userUpdated = false;

                // Ensure Role
                if (string.IsNullOrEmpty(user.RoleId))
                {
                    user.RoleId = user.Username.ToLower().Contains("admin") ? (adminRole?.Id ?? "") : (empRole?.Id ?? "");
                    userUpdated = true;
                }

                // Ensure Department
                if (string.IsNullOrEmpty(user.DepartmentId))
                {
                    user.DepartmentId = dept.Id!;
                    userUpdated = true;
                }

                // Update user if needed
                if (userUpdated)
                {
                    await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
                    userFixes++;
                }

                // Ensure Employee Record
                var existingEmp = await _context.Employees.Find(e => e.UserId == user.Id).FirstOrDefaultAsync();
                if (existingEmp == null)
                {
                    var newEmp = new Models.Employee
                    {
                        UserId = user.Id!,
                        DepartmentId = user.DepartmentId!,
                        FirstName = user.FirstName ?? user.Username,
                        LastName = user.LastName ?? "User",
                        Email = user.Email ?? $"{user.Username}@company.com",
                        Position = user.Position ?? "Staff",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.Employees.InsertOneAsync(newEmp);
                    reports.Add($"✅ Created Employee record for {user.Username}");
                }

                // Ensure ALL Leave Balances for this user
                foreach (var type in allLeaveTypes)
                {
                    var existingBal = await _context.LeaveBalances.Find(
                        b => b.EmployeeId == user.Id && b.LeaveTypeId == type.Id && b.Year == DateTime.UtcNow.Year
                    ).AnyAsync();

                    if (!existingBal)
                    {
                        var quota = defaultLeaveTypes.FirstOrDefault(d => d.Name == type.TypeName)?.Days ?? 10;
                        if (type.TypeName == "Annual Leave" && user.AnnualLeaveQuota.HasValue)
                        {
                            quota = user.AnnualLeaveQuota.Value;
                        }

                        var balance = new Models.LeaveBalance
                        {
                            EmployeeId = user.Id!,
                            LeaveTypeId = type.Id!,
                            Year = DateTime.UtcNow.Year,
                            TotalDays = quota,
                            UsedDays = 0,
                            RemainingDays = quota
                        };
                        await _context.LeaveBalances.InsertOneAsync(balance);
                        balanceFixes++;
                    }
                }
            }

            reports.Add($"📊 Fixed {userFixes} users and initialized {balanceFixes} leave balances.");

            return Ok(new
            {
                Message = "🔥 Force Seed Completed",
                Reports = reports
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Force Seed failed");
            return StatusCode(500, new { Error = "Force Seed failed", Message = ex.Message });
        }
    }
}
