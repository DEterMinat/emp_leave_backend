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

        // Ensure user hasn't already checked in today
        var existing = await _context.Attendances
            .Find(a => a.EmployeeID == dto.EmployeeID && a.AttendanceDate == today)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            throw new InvalidOperationException("Already checked in today.");
        }

        var attendance = new Attendance
        {
            EmployeeID = dto.EmployeeID,
            AttendanceDate = today,
            CheckInTime = DateTime.UtcNow,
            Status = "Present", // Can add logic for Late based on time
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Attendances.InsertOneAsync(attendance);

        return MapToDto(attendance);
    }

    public async Task<AttendanceDto> CheckOutAsync(CheckOutDto dto)
    {
        var today = DateTime.UtcNow.Date;

        var existing = await _context.Attendances
            .Find(a => a.EmployeeID == dto.EmployeeID && a.AttendanceDate == today)
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

        return MapToDto(updated);
    }

    public async Task<List<AttendanceDto>> GetHistoryByEmployeeIdAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var filterBuilder = Builders<Attendance>.Filter;
        var filter = filterBuilder.Eq(a => a.EmployeeID, employeeId);

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

        return records.Select(MapToDto).ToList();
    }

    public async Task<AttendanceDto?> GetTodayAttendanceAsync(string employeeId)
    {
        var today = DateTime.UtcNow.Date;
        var attendance = await _context.Attendances
            .Find(a => a.EmployeeID == employeeId && a.AttendanceDate == today)
            .FirstOrDefaultAsync();

        return attendance != null ? MapToDto(attendance) : null;
    }

    private static AttendanceDto MapToDto(Attendance a)
    {
        return new AttendanceDto
        {
            AttendanceID = a.AttendanceID!,
            EmployeeID = a.EmployeeID,
            AttendanceDate = a.AttendanceDate,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            Status = a.Status,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }
}
