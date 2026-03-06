using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Services;

public interface IAttendanceService
{
    Task<AttendanceDto> CheckInAsync(CheckInDto dto);
    Task<AttendanceDto> CheckOutAsync(CheckOutDto dto);
    Task<List<AttendanceDto>> GetHistoryByEmployeeIdAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null);
    Task<AttendanceDto?> GetTodayAttendanceAsync(string employeeId);
    Task<List<AttendanceDto>> GetAllAttendanceAsync(DateTime? date = null);
}
