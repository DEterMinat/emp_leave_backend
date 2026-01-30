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
            CreatedAt = DateTime.UtcNow
        };
        
        await _context.Users.InsertOneAsync(user);
        
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
        return new UserResponseDto
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
            AnnualLeaveQuota = u.AnnualLeaveQuota
            // ---------------------------------------
        };
    }
}
