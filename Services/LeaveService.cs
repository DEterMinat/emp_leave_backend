using MongoDB.Driver;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;

namespace EmployeeLeaveApi.Services;

public class LeaveService : ILeaveService
{
    private readonly MongoDbContext _context;

    public LeaveService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveRequestDto>> GetAllAsync(string? status = null)
    {
        var filter = status != null 
            ? Builders<LeaveRequest>.Filter.Eq(r => r.Status, status)
            : Builders<LeaveRequest>.Filter.Empty;
            
        var requests = await _context.LeaveRequests.Find(filter).ToListAsync();
        var dtos = new List<LeaveRequestDto>();
        
        foreach (var r in requests)
        {
            dtos.Add(await MapToDto(r));
        }
        
        return dtos;
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(string id)
    {
        var r = await _context.LeaveRequests.Find(req => req.Id == id).FirstOrDefaultAsync();
        if (r == null) return null;
        return await MapToDto(r);
    }

    public async Task<List<LeaveRequestDto>> GetByEmployeeIdAsync(string employeeId)
    {
        var requests = await _context.LeaveRequests.Find(r => r.EmployeeId == employeeId).ToListAsync();
        var dtos = new List<LeaveRequestDto>();
        foreach (var r in requests)
        {
            dtos.Add(await MapToDto(r));
        }
        return dtos;
    }

    public async Task<LeaveRequestDto> CreateAsync(LeaveRequestCreateDto dto)
    {
        var totalDays = (int)(dto.EndDate - dto.StartDate).TotalDays + 1;
        
        var request = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalDays = totalDays,
            Reason = dto.Reason,
            Status = "Pending",
            RequestedDate = DateTime.UtcNow
        };
        
        await _context.LeaveRequests.InsertOneAsync(request);
        return await MapToDto(request);
    }

    public async Task<LeaveRequestDto?> ApproveAsync(string id, LeaveRequestUpdateDto dto)
    {
        var update = Builders<LeaveRequest>.Update
            .Set(r => r.Status, "Approved")
            .Set(r => r.Comment, dto.Comment)
            .Set(r => r.ApproverId, dto.ApproverId)
            .Set(r => r.ApprovedDate, DateTime.UtcNow);
        
        var result = await _context.LeaveRequests.UpdateOneAsync(r => r.Id == id, update);
        if (result.MatchedCount == 0) return null;
        
        // Update leave balance
        var request = await _context.LeaveRequests.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (request != null)
        {
            var year = DateTime.UtcNow.Year;
            
            // Check if balance exists, if not create one (optional safeguard)
            var balance = await _context.LeaveBalances.Find(
                b => b.EmployeeId == request.EmployeeId && b.LeaveTypeId == request.LeaveTypeId && b.Year == year
            ).FirstOrDefaultAsync();

            if (balance != null)
            {
                var balanceUpdate = Builders<LeaveBalance>.Update
                    .Inc(b => b.UsedDays, request.TotalDays)
                    .Inc(b => b.RemainingDays, -request.TotalDays);
                
                await _context.LeaveBalances.UpdateOneAsync(b => b.Id == balance.Id, balanceUpdate);
            }
        }
        
        return await GetByIdAsync(id);
    }

    public async Task<LeaveRequestDto?> RejectAsync(string id, LeaveRequestUpdateDto dto)
    {
        var update = Builders<LeaveRequest>.Update
            .Set(r => r.Status, "Rejected")
            .Set(r => r.Comment, dto.Comment)
            .Set(r => r.ApproverId, dto.ApproverId)
            .Set(r => r.ApprovedDate, DateTime.UtcNow);
        
        var result = await _context.LeaveRequests.UpdateOneAsync(r => r.Id == id, update);
        if (result.MatchedCount == 0) return null;
        
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _context.LeaveRequests.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0;
    }

    private async Task<LeaveRequestDto> MapToDto(LeaveRequest r)
    {
        var emp = await _context.Employees.Find(e => e.Id == r.EmployeeId).FirstOrDefaultAsync();
        var type = await _context.LeaveTypes.Find(t => t.Id == r.LeaveTypeId).FirstOrDefaultAsync();
        
        return new LeaveRequestDto
        {
            Id = r.Id!,
            EmployeeId = r.EmployeeId,
            LeaveTypeId = r.LeaveTypeId,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            TotalDays = r.TotalDays,
            Reason = r.Reason,
            Status = r.Status,
            Comment = r.Comment,
            RequestedDate = r.RequestedDate,
            ApproverId = r.ApproverId,
            ApprovedDate = r.ApprovedDate,
            EmployeeName = emp != null ? $"{emp.FirstName} {emp.LastName}" : null,
            LeaveTypeName = type?.TypeName
        };
    }
}
