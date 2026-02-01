using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;
using EmployeeLeaveApi.Helpers;

namespace EmployeeLeaveApi.Services;

public class UserService : IUserService
{
    private readonly IMongoDbContext _context;

    public UserService(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        var users = await _context.Users.Find(_ => true).ToListAsync();
        var dtos = new List<UserResponseDto>();

        foreach (var u in users)
        {
            dtos.Add(await MapToDto(u));
        }

        return dtos;
    }

    public async Task<UserResponseDto?> GetByIdAsync(string id)
    {
        var user = await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return null;
        return await MapToDto(user);
    }

    public async Task<UserResponseDto> CreateAsync(UserCreateDto dto)
    {
        var hashedPassword = PasswordHelper.HashPassword(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            Password = hashedPassword,
            RoleId = dto.RoleId,
            Email = dto.Email,
            Phone = dto.Phone,
            AnnualLeaveQuota = dto.AnnualLeaveQuota,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DepartmentId = dto.DepartmentId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.InsertOneAsync(user);

        // --- One-Stop Setup: Create Employee Profile & Initialize Balances ---
        if (!string.IsNullOrEmpty(dto.DepartmentId))
        {
            var employee = new Employee
            {
                UserId = user.Id!,
                DepartmentId = dto.DepartmentId,
                FirstName = dto.FirstName ?? user.Username,
                LastName = dto.LastName ?? "",
                Email = dto.Email ?? "",
                Phone = dto.Phone,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Employees.InsertOneAsync(employee);

            // Initialize Leave Balances
            var leaveTypes = await _context.LeaveTypes.Find(_ => true).ToListAsync();
            var year = DateTime.UtcNow.Year;
            var defaultDays = new Dictionary<string, int>
            {
                { "Annual Leave", dto.AnnualLeaveQuota ?? 10 },
                { "Sick Leave", 30 },
                { "Personal Leave", 3 }
            };

            foreach (var type in leaveTypes)
            {
                var totalDays = defaultDays.ContainsKey(type.TypeName)
                    ? defaultDays[type.TypeName]
                    : (type.TypeName.Contains("Annual") ? (dto.AnnualLeaveQuota ?? 10) : 10);

                var balance = new LeaveBalance
                {
                    EmployeeId = user.Id!, // Note: Some parts of the system use UserId as EmployeeId, others use MongoDB's Employee.Id. 
                                           // Looking at LeaveBalancesController, it uses 'EmployeeId'. 
                                           // But in Dashboard, it queries by UserId. 
                                           // I will stick to UserId for now as it's the current pattern in LeaveBalancesController.mine
                    LeaveTypeId = type.Id!,
                    Year = year,
                    TotalDays = totalDays,
                    UsedDays = 0,
                    RemainingDays = totalDays
                };
                await _context.LeaveBalances.InsertOneAsync(balance);
            }
        }
        // ------------------------------------------------------------------

        // Ensure to return DTO with RoleName
        return await MapToDto(user);
    }

    public async Task<UserResponseDto?> UpdateAsync(string id, UserUpdateDto dto)
    {
        var update = Builders<User>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(dto.Username)) update = update.Set(u => u.Username, dto.Username);

        if (!string.IsNullOrEmpty(dto.Password))
        {
            var hashedPassword = PasswordHelper.HashPassword(dto.Password);
            update = update.Set(u => u.Password, hashedPassword);
        }

        if (!string.IsNullOrEmpty(dto.RoleId)) update = update.Set(u => u.RoleId, dto.RoleId);

        // --- เพิ่มส่วนนี้เพื่อให้บันทึกข้อมูลใหม่ได้ ---
        if (!string.IsNullOrEmpty(dto.Email)) update = update.Set(u => u.Email, dto.Email);
        if (!string.IsNullOrEmpty(dto.Phone)) update = update.Set(u => u.Phone, dto.Phone);
        if (dto.AnnualLeaveQuota.HasValue) update = update.Set(u => u.AnnualLeaveQuota, dto.AnnualLeaveQuota.Value);
        if (!string.IsNullOrEmpty(dto.FirstName)) update = update.Set(u => u.FirstName, dto.FirstName);
        if (!string.IsNullOrEmpty(dto.LastName)) update = update.Set(u => u.LastName, dto.LastName);
        if (!string.IsNullOrEmpty(dto.DepartmentId)) update = update.Set(u => u.DepartmentId, dto.DepartmentId);
        // ---------------------------------------

        var result = await _context.Users.UpdateOneAsync(u => u.Id == id, update);
        if (result.MatchedCount == 0) return null;

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _context.Users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.Find(u => u.Username == username).FirstOrDefaultAsync();
    }

    private async Task<UserResponseDto> MapToDto(User u)
    {
        var role = await _context.Roles.Find(r => r.Id == u.RoleId).FirstOrDefaultAsync();
        var res = new UserResponseDto
        {
            Id = u.Id!,
            Username = u.Username,
            RoleId = u.RoleId,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            RoleName = role?.RoleName,
            // --- เพิ่มการ Map ฟิลด์ใหม่กลับไปยัง DTO ---
            Email = u.Email,
            Phone = u.Phone,
            AnnualLeaveQuota = u.AnnualLeaveQuota,
            FirstName = u.FirstName,
            LastName = u.LastName,
            DepartmentId = u.DepartmentId
            // ---------------------------------------
        };

        if (!string.IsNullOrEmpty(u.DepartmentId))
        {
            var dept = await _context.Departments.Find(d => d.Id == u.DepartmentId).FirstOrDefaultAsync();
            res.DepartmentName = dept?.DepartmentName;
        }

        return res;
    }
}
