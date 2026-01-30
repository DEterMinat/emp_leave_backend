using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Services;

public interface IUserService
{
    Task<List<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto?> GetByIdAsync(string id);
    Task<UserResponseDto> CreateAsync(UserCreateDto dto);
    Task<UserResponseDto?> UpdateAsync(string id, UserUpdateDto dto);
    Task<bool> DeleteAsync(string id);
    Task<User?> GetByUsernameAsync(string username);
}
