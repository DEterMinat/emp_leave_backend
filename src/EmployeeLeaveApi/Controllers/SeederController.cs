using Microsoft.AspNetCore.Mvc;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Models;
using MongoDB.Driver;

namespace EmployeeLeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeederController : ControllerBase
{
    private readonly MongoDbContext _context;

    public SeederController(MongoDbContext context)
    {
        _context = context;
    }

    [HttpPost("seed-missing-collections")]
    public async Task<IActionResult> SeedMissingCollections()
    {
        // 1. Employees (ต้องการ DepartmentId และ UserId ที่มีอยู่จริง หรือสร้าง Dummy)
        // เพื่อความง่าย เราจะเช็คก่อนว่ามี Dept ไหม ถ้าไม่มีสร้าง Dummy Dept
        var dept = await _context.Departments.Find(_ => true).FirstOrDefaultAsync();
        if (dept == null)
        {
            dept = new Department { DepartmentName = "IT (Seeded)" };
            await _context.Departments.InsertOneAsync(dept);
        }

        var countEmp = await _context.Employees.CountDocumentsAsync(_ => true);
        if (countEmp == 0)
        {
            var emp = new Employee
            {
                FirstName = "System",
                LastName = "Admin",
                Email = "admin@system.com",
                Phone = "0000000000",
                Address = "System",
                DepartmentId = dept.Id!,
                UserId = "seed_user_id", // Dummy
                CreatedAt = DateTime.UtcNow
            };
            await _context.Employees.InsertOneAsync(emp);
        }

        // 2. LeaveRequests
        var countReq = await _context.LeaveRequests.CountDocumentsAsync(_ => true);
        if (countReq == 0)
        {
            await _context.LeaveRequests.InsertOneAsync(new LeaveRequest
            {
                EmployeeId = "seed_emp_id",
                LeaveTypeId = "seed_type_id",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                TotalDays = 1,
                Reason = "Seeding Collection",
                Status = "Pending",
                RequestedDate = DateTime.UtcNow
            });
        }

        // 3. LeaveBalances
        var countBal = await _context.LeaveBalances.CountDocumentsAsync(_ => true);
        if (countBal == 0)
        {
            await _context.LeaveBalances.InsertOneAsync(new LeaveBalance
            {
                EmployeeId = "seed_emp_id",
                LeaveTypeId = "seed_type_id",
                Year = DateTime.UtcNow.Year,
                TotalDays = 10,
                UsedDays = 0,
                RemainingDays = 10
            });
        }

        // 4. LeaveAttachments
        var countAttach = await _context.LeaveAttachments.CountDocumentsAsync(_ => true);
        if (countAttach == 0)
        {
            await _context.LeaveAttachments.InsertOneAsync(new LeaveAttachment
            {
                RequestId = "seed_req_id",
                FileName = "seed.pdf",
                FilePath = "/uploads/seed.pdf",
                UploadedDate = DateTime.UtcNow
            });
        }

        return Ok(new { message = "✅ All missing collections seeded successfully!" });
    }
}
