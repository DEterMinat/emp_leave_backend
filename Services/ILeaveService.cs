using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Services;

public interface ILeaveService
{
    Task<List<LeaveRequestDto>> GetAllAsync(string? status = null);
    Task<LeaveRequestDto?> GetByIdAsync(string id);
    Task<List<LeaveRequestDto>> GetByEmployeeIdAsync(string employeeId);
    Task<LeaveRequestDto> CreateAsync(LeaveRequestCreateDto dto);
    Task<LeaveRequestDto?> ApproveAsync(string id, LeaveRequestUpdateDto dto);
    Task<LeaveRequestDto?> RejectAsync(string id, LeaveRequestUpdateDto dto);
    Task<bool> DeleteAsync(string id);
}
