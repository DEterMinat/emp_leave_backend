using EmployeeLeaveApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.DTOs;
using EmployeeLeaveApi.Models;
using MongoDB.Driver;

namespace EmployeeLeaveApi.Services;


public class LeaveService : ILeaveService
{
    private readonly IMongoDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public LeaveService(IMongoDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
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
        var resDto = await MapToDto(request);

        // Notify Managers/HR
        await _hubContext.Clients.Group("Managers")
            .SendAsync("ReceiveNotification", "New Leave Request", $"New request from {resDto.EmployeeName} for {resDto.LeaveTypeName}");

        return resDto;
    }

    public async Task<LeaveRequestDto> CreateWithFileAsync(LeaveRequestCreateDto dto, Stream? fileStream, string? fileName)
    {
        // 1. Create Leave Request first
        var requestDto = await CreateAsync(dto);
        
        if (fileStream != null && !string.IsNullOrEmpty(fileName))
        {
            // 2. Save File
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
            
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(uploadsDir, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream);
            }
            
            // 3. Create Attachment Record
            var attachment = new LeaveAttachment
            {
                RequestId = requestDto.Id,
                FileName = fileName,
                FilePath = $"/uploads/{uniqueFileName}", // Relative path for serving
                UploadedDate = DateTime.UtcNow
            };
            
            await _context.LeaveAttachments.InsertOneAsync(attachment);
        }
        
        return requestDto;
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
        
        
        var updatedRequest = await GetByIdAsync(id);
        if (updatedRequest != null)
        {
            // Send real-time notification to the employee
            await _hubContext.Clients.Group(updatedRequest.EmployeeId)
                .SendAsync("ReceiveNotification", "Leave Approved", $"Your leave request for {updatedRequest.LeaveTypeName} has been Approved.");
        }

        return updatedRequest;
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
        
        
        var updatedRequest = await GetByIdAsync(id);
        if (updatedRequest != null)
        {
            // Send real-time notification to the employee
            await _hubContext.Clients.Group(updatedRequest.EmployeeId)
                .SendAsync("ReceiveNotification", "Leave Rejected", $"Your leave request for {updatedRequest.LeaveTypeName} has been Rejected.");
        }

        return updatedRequest;
    }


    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _context.LeaveRequests.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<List<LeaveAttachmentDto>> GetAttachmentsAsync(string requestId)
    {
        var attachments = await _context.LeaveAttachments.Find(a => a.RequestId == requestId).ToListAsync();
        return attachments.Select(a => new LeaveAttachmentDto
        {
            Id = a.Id!,
            RequestId = a.RequestId,
            FileName = a.FileName,
            FilePath = a.FilePath,
            UploadedDate = a.UploadedDate
        }).ToList();
    }

    private async Task<LeaveRequestDto> MapToDto(LeaveRequest r)
    {
        var emp = await _context.Users.Find(e => e.Id == r.EmployeeId).FirstOrDefaultAsync();
        var type = await _context.LeaveTypes.Find(t => t.Id == r.LeaveTypeId).FirstOrDefaultAsync();
        var hasAttachments = await _context.LeaveAttachments.Find(a => a.RequestId == r.Id).AnyAsync();
        
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
            EmployeeName = emp != null ? emp.Username : "Unknown User",
            LeaveTypeName = type?.TypeName,
            HasAttachments = hasAttachments
        };
    }
}
