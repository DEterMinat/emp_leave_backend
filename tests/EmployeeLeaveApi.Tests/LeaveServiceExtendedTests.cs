using Moq;
using MongoDB.Driver;
using Microsoft.AspNetCore.SignalR;
using EmployeeLeaveApi.Services;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Models;
using EmployeeLeaveApi.Hubs;
using EmployeeLeaveApi.DTOs;

namespace EmployeeLeaveApi.Tests;

public class LeaveServiceExtendedTests
{
    private readonly Mock<IMongoDbContext> _mockContext;
    private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
    private readonly Mock<IMongoCollection<LeaveRequest>> _mockLeaveRequests;
    private readonly Mock<IMongoCollection<LeaveBalance>> _mockLeaveBalances;
    private readonly Mock<IMongoCollection<User>> _mockUsers;
    private readonly Mock<IMongoCollection<LeaveType>> _mockLeaveTypes;
    private readonly Mock<IMongoCollection<LeaveAttachment>> _mockLeaveAttachments;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly LeaveService _service;

    public LeaveServiceExtendedTests()
    {
        _mockContext = new Mock<IMongoDbContext>();
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockLeaveRequests = new Mock<IMongoCollection<LeaveRequest>>();
        _mockLeaveBalances = new Mock<IMongoCollection<LeaveBalance>>();
        _mockUsers = new Mock<IMongoCollection<User>>();
        _mockLeaveTypes = new Mock<IMongoCollection<LeaveType>>();
        _mockLeaveAttachments = new Mock<IMongoCollection<LeaveAttachment>>();
        _mockNotificationService = new Mock<INotificationService>();

        _mockContext.Setup(c => c.LeaveRequests).Returns(_mockLeaveRequests.Object);
        _mockContext.Setup(c => c.LeaveBalances).Returns(_mockLeaveBalances.Object);
        _mockContext.Setup(c => c.Users).Returns(_mockUsers.Object);
        _mockContext.Setup(c => c.LeaveTypes).Returns(_mockLeaveTypes.Object);
        _mockContext.Setup(c => c.LeaveAttachments).Returns(_mockLeaveAttachments.Object);

        // Setup SignalR mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        _service = new LeaveService(_mockContext.Object, _mockHubContext.Object, _mockNotificationService.Object);
    }

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_ShouldThrow()
    {
        // Arrange
        var dto = new LeaveRequestCreateDto
        {
            EmployeeId = "emp-1",
            LeaveTypeId = "type-1",
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(1), // End before Start
            Reason = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ShouldReturnDto()
    {
        // Arrange
        var dto = new LeaveRequestCreateDto
        {
            EmployeeId = "emp-1",
            LeaveTypeId = "type-1",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Family vacation"
        };

        _mockLeaveRequests.Setup(c => c.InsertOneAsync(
            It.IsAny<LeaveRequest>(), null, default))
            .Returns(Task.CompletedTask);

        SetupMockCursor(_mockUsers, new List<User>
        {
            new User { Id = "emp-1", Username = "john.doe" }
        });
        SetupMockCursor(_mockLeaveTypes, new List<LeaveType>
        {
            new LeaveType { Id = "type-1", TypeName = "Annual Leave" }
        });
        SetupMockCursor(_mockLeaveAttachments, new List<LeaveAttachment>());

        // Mock SignalR SendCoreAsync (SendAsync is an extension that calls SendCoreAsync)
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClientProxy.Setup(c => c.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(3, result.TotalDays);
        Assert.Equal("Annual Leave", result.LeaveTypeName);
    }

    // ==================== RejectAsync ====================

    [Fact]
    public async Task RejectAsync_RequestNotFound_ShouldReturnNull()
    {
        // Arrange
        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest>());

        // Act
        var result = await _service.RejectAsync("nonexistent", new LeaveRequestUpdateDto());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RejectAsync_NotPendingStatus_ShouldThrow()
    {
        // Arrange
        var request = new LeaveRequest
        {
            Id = "req-1",
            EmployeeId = "emp-1",
            LeaveTypeId = "type-1",
            TotalDays = 2,
            Status = "Approved" // Not Pending!
        };

        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest> { request });

        var dto = new LeaveRequestUpdateDto { Comment = "Changed mind", ApproverId = "admin" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RejectAsync("req-1", dto));
    }

    // ==================== ApproveAsync ====================

    [Fact]
    public async Task ApproveAsync_RequestNotFound_ShouldReturnNull()
    {
        // Arrange
        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest>());

        // Act
        var result = await _service.ApproveAsync("nonexistent", new LeaveRequestUpdateDto());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveAsync_NotPendingStatus_ShouldThrow()
    {
        // Arrange
        var request = new LeaveRequest
        {
            Id = "req-1",
            EmployeeId = "emp-1",
            LeaveTypeId = "type-1",
            TotalDays = 2,
            Status = "Rejected" // Not Pending!
        };

        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest> { request });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveAsync("req-1", new LeaveRequestUpdateDto()));
    }

    [Fact]
    public async Task ApproveAsync_InsufficientBalance_ShouldThrow()
    {
        // Arrange
        var request = new LeaveRequest
        {
            Id = "req-1",
            EmployeeId = "emp-1",
            LeaveTypeId = "type-1",
            TotalDays = 5,
            Status = "Pending"
        };

        var balance = new LeaveBalance
        {
            Id = "bal-1",
            EmployeeId = "emp-1",
            LeaveTypeId = "type-1",
            Year = DateTime.UtcNow.Year,
            RemainingDays = 2, // Only 2 remaining, need 5
            UsedDays = 8
        };

        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest> { request });
        SetupMockCursor(_mockLeaveBalances, new List<LeaveBalance> { balance });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveAsync("req-1", new LeaveRequestUpdateDto()));
    }

    [Fact]
    public async Task ApproveAsync_NoBalanceRecord_ShouldThrow()
    {
        // Arrange
        var request = new LeaveRequest
        {
            Id = "req-1",
            EmployeeId = "emp-1",
            LeaveTypeId = "type-1",
            TotalDays = 2,
            Status = "Pending"
        };

        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest> { request });
        SetupMockCursor(_mockLeaveBalances, new List<LeaveBalance>()); // No balance!

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveAsync("req-1", new LeaveRequestUpdateDto()));
    }

    // ==================== GetByIdAsync ====================

    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldReturnNull()
    {
        // Arrange
        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest>());

        // Act
        var result = await _service.GetByIdAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    // ==================== GetByEmployeeIdAsync ====================

    [Fact]
    public async Task GetByEmployeeIdAsync_NoRequests_ShouldReturnEmptyList()
    {
        // Arrange
        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest>());

        // Act
        var result = await _service.GetByEmployeeIdAsync("emp-1");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_Exists_ShouldReturnTrue()
    {
        // Arrange
        _mockLeaveRequests.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<LeaveRequest>>(),
            default))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        // Act
        var result = await _service.DeleteAsync("req-1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockLeaveRequests.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<LeaveRequest>>(),
            default))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        // Act
        var result = await _service.DeleteAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    // ==================== GetAttachmentsAsync ====================

    [Fact]
    public async Task GetAttachmentsAsync_NoAttachments_ShouldReturnEmptyList()
    {
        // Arrange
        SetupMockCursor(_mockLeaveAttachments, new List<LeaveAttachment>());

        // Act
        var result = await _service.GetAttachmentsAsync("req-1");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ==================== Helper ====================

    private void SetupMockCursor<T>(Mock<IMongoCollection<T>> mockCollection, List<T> results)
    {
        var mockCursor = new Mock<IAsyncCursor<T>>();
        mockCursor.Setup(_ => _.Current).Returns(results);
        mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        mockCollection.Setup(c => c.FindAsync(
            It.IsAny<FilterDefinition<T>>(),
            It.IsAny<FindOptions<T, T>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);
    }
}
