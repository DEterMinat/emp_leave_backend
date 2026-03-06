using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IMongoDbContext _context;

    public AttendanceService(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceDto> CheckInAsync(CheckInDto dto)
    {
        var today = DateTime.UtcNow.Date;
        var resolvedEmployeeId = await ResolveOrCreateEmployeeIdAsync(dto.EmployeeID);

        if (string.IsNullOrEmpty(resolvedEmployeeId))
        {
            throw new InvalidOperationException("Employee profile not found for the current user.");
        }

        // Ensure user hasn't already checked in today
        var existing = await _context.Attendances
            .Find(a => a.EmployeeID == resolvedEmployeeId && a.AttendanceDate == today)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            throw new InvalidOperationException("Already checked in today.");
        }

        var now = DateTime.UtcNow;
        var status = now.TimeOfDay > new TimeSpan(9, 0, 0) ? "Late" : "Present";

        var attendance = new Attendance
        {
            EmployeeID = resolvedEmployeeId,
            AttendanceDate = today,
            CheckInTime = now,
            Status = status,
            Notes = dto.Notes,
            CreatedAt = now
        };

        await _context.Attendances.InsertOneAsync(attendance);
        var employee = await _context.Employees.Find(e => e.Id == resolvedEmployeeId).FirstOrDefaultAsync();

        return MapToDto(attendance, employee != null ? $"{employee.FirstName} {employee.LastName}" : null);
    }

    public async Task<AttendanceDto> CheckOutAsync(CheckOutDto dto)
    {
        var today = DateTime.UtcNow.Date;
        var resolvedEmployeeId = await ResolveEmployeeIdOrNullAsync(dto.EmployeeID);

        if (string.IsNullOrEmpty(resolvedEmployeeId))
        {
            throw new InvalidOperationException("Employee profile not found for the current user.");
        }

        var existing = await _context.Attendances
            .Find(a => a.EmployeeID == resolvedEmployeeId && a.AttendanceDate == today)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            throw new InvalidOperationException("Not checked in today.");
        }

        if (existing.CheckOutTime != null)
        {
            throw new InvalidOperationException("Already checked out today.");
        }

        var update = Builders<Attendance>.Update
            .Set(a => a.CheckOutTime, DateTime.UtcNow)
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(dto.Notes))
        {
            update = update.Set(a => a.Notes, existing.Notes + " | " + dto.Notes);
        }

        var options = new FindOneAndUpdateOptions<Attendance> { ReturnDocument = ReturnDocument.After };
        var updated = await IMongoCollectionExtensions.FindOneAndUpdateAsync<Attendance>(
            _context.Attendances,
            a => a.AttendanceID == existing.AttendanceID,
            update,
            options
        );

        var employee = await _context.Employees.Find(e => e.Id == resolvedEmployeeId).FirstOrDefaultAsync();
        return MapToDto(updated, employee != null ? $"{employee.FirstName} {employee.LastName}" : null);
    }

    public async Task<List<AttendanceDto>> GetHistoryByEmployeeIdAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var resolvedEmployeeId = await ResolveEmployeeIdOrNullAsync(employeeId);
        if (string.IsNullOrEmpty(resolvedEmployeeId))
        {
            return new List<AttendanceDto>();
        }

        var filterBuilder = Builders<Attendance>.Filter;
        var filter = filterBuilder.Eq(a => a.EmployeeID, resolvedEmployeeId);

        if (startDate.HasValue)
        {
            filter &= filterBuilder.Gte(a => a.AttendanceDate, startDate.Value.Date);
        }
        
        if (endDate.HasValue)
        {
            filter &= filterBuilder.Lte(a => a.AttendanceDate, endDate.Value.Date);
        }

        var records = await _context.Attendances
            .Find(filter)
            .SortByDescending(a => a.AttendanceDate)
            .ToListAsync();

        var employee = await _context.Employees.Find(e => e.Id == resolvedEmployeeId).FirstOrDefaultAsync();
        var name = employee != null ? $"{employee.FirstName} {employee.LastName}" : null;

        return records.Select(r => MapToDto(r, name)).ToList();
    }

    public async Task<AttendanceDto?> GetTodayAttendanceAsync(string employeeId)
    {
        var resolvedEmployeeId = await ResolveEmployeeIdOrNullAsync(employeeId);
        if (string.IsNullOrEmpty(resolvedEmployeeId))
        {
            return null;
        }

        var today = DateTime.UtcNow.Date;
        var attendance = await _context.Attendances
            .Find(a => a.EmployeeID == resolvedEmployeeId && a.AttendanceDate == today)
            .FirstOrDefaultAsync();

        if (attendance == null) return null;

        var employee = await _context.Employees.Find(e => e.Id == resolvedEmployeeId).FirstOrDefaultAsync();
        return MapToDto(attendance, employee != null ? $"{employee.FirstName} {employee.LastName}" : null);
    }

    public async Task<List<AttendanceDto>> GetAllAttendanceAsync(DateTime? date = null)
    {
        var targetDate = date?.Date ?? DateTime.UtcNow.Date;
        var filter = Builders<Attendance>.Filter.Eq(a => a.AttendanceDate, targetDate);
        
        var records = await _context.Attendances
            .Find(filter)
            .ToListAsync();

        if (!records.Any()) return new List<AttendanceDto>();

        var employeeIds = records.Select(r => r.EmployeeID).Distinct().ToList();
        var employees = await _context.Employees
            .Find(Builders<Employee>.Filter.In(e => e.Id, employeeIds))
            .ToListAsync();

        var employeeMap = employees.ToDictionary(e => e.Id!, e => $"{e.FirstName} {e.LastName}");

        return records.Select(r => MapToDto(r, employeeMap.GetValueOrDefault(r.EmployeeID))).ToList();
    }

    private AttendanceDto MapToDto(Attendance a, string? employeeName = null)
    {
        return new AttendanceDto
        {
            AttendanceID = a.AttendanceID!,
            EmployeeID = a.EmployeeID,
            EmployeeName = employeeName,
            AttendanceDate = a.AttendanceDate,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            Status = a.Status,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }

    private async Task<string?> ResolveEmployeeIdOrNullAsync(string? employeeIdentifier)
    {
        if (string.IsNullOrWhiteSpace(employeeIdentifier))
        {
            return null;
        }

        var employee = await _context.Employees
            .Find(e => e.Id == employeeIdentifier || e.UserId == employeeIdentifier)
            .FirstOrDefaultAsync();

        return employee?.Id;
    }

    private async Task<string?> ResolveOrCreateEmployeeIdAsync(string? employeeIdentifier)
    {
        var resolvedEmployeeId = await ResolveEmployeeIdOrNullAsync(employeeIdentifier);
        if (!string.IsNullOrEmpty(resolvedEmployeeId))
        {
            return resolvedEmployeeId;
        }

        return await CreateEmployeeFromUserIdentifierAsync(employeeIdentifier);
    }

    private async Task<string?> CreateEmployeeFromUserIdentifierAsync(string? employeeIdentifier)
    {
        if (string.IsNullOrWhiteSpace(employeeIdentifier))
        {
            return null;
        }

        var user = await _context.Users
            .Find(u => u.Id == employeeIdentifier)
            .FirstOrDefaultAsync();

        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return null;
        }

        var departmentId = await EnsureDepartmentIdAsync(user.DepartmentId);
        var firstName = string.IsNullOrWhiteSpace(user.FirstName) ? user.Username : user.FirstName;
        var email = string.IsNullOrWhiteSpace(user.Email) ? $"{user.Username}@company.local" : user.Email;

        var newEmployee = new Employee
        {
            UserId = user.Id,
            DepartmentId = departmentId,
            FirstName = firstName,
            LastName = string.IsNullOrWhiteSpace(user.LastName) ? "User" : user.LastName,
            Email = email,
            Phone = user.Phone,
            Position = "Employee",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Employees.InsertOneAsync(newEmployee);
        if (!string.IsNullOrEmpty(newEmployee.Id))
        {
            return newEmployee.Id;
        }

        var createdEmployee = await _context.Employees
            .Find(e => e.UserId == user.Id)
            .SortByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        return createdEmployee?.Id;
    }

    private async Task<string> EnsureDepartmentIdAsync(string? preferredDepartmentId)
    {
        if (!string.IsNullOrWhiteSpace(preferredDepartmentId))
        {
            var preferredDepartment = await _context.Departments
                .Find(d => d.Id == preferredDepartmentId)
                .FirstOrDefaultAsync();

            if (preferredDepartment != null && !string.IsNullOrEmpty(preferredDepartment.Id))
            {
                return preferredDepartment.Id;
            }
        }

        var firstDepartment = await _context.Departments
            .Find(_ => true)
            .FirstOrDefaultAsync();

        if (firstDepartment != null && !string.IsNullOrEmpty(firstDepartment.Id))
        {
            return firstDepartment.Id;
        }

        var generalDepartment = new Department
        {
            DepartmentName = "General"
        };

        await _context.Departments.InsertOneAsync(generalDepartment);

        if (string.IsNullOrEmpty(generalDepartment.Id))
        {
            throw new InvalidOperationException("Failed to create default department.");
        }

        return generalDepartment.Id;
    }
}
