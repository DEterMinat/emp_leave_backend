using Moq;
using Xunit;
using System.Linq;
using MongoDB.Driver;
using Microsoft.AspNetCore.SignalR;
using EmployeeLeaveApi.Services;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Models;
using EmployeeLeaveApi.Hubs;
using EmployeeLeaveApi.DTOs;

namespace EmployeeLeaveApi.Tests;

public class LeaveServiceTests
{
    private readonly Mock<IMongoDbContext> _mockContext;
    private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
    private readonly Mock<IMongoCollection<LeaveRequest>> _mockLeaveRequests;
    private readonly Mock<IMongoCollection<LeaveBalance>> _mockLeaveBalances;
    private readonly Mock<IMongoCollection<User>> _mockUsers;
    private readonly Mock<IMongoCollection<LeaveType>> _mockLeaveTypes;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly LeaveService _service;

    public LeaveServiceTests()
    {
        _mockContext = new Mock<IMongoDbContext>();
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockLeaveRequests = new Mock<IMongoCollection<LeaveRequest>>();
        _mockLeaveBalances = new Mock<IMongoCollection<LeaveBalance>>();
        _mockUsers = new Mock<IMongoCollection<User>>();
        _mockLeaveTypes = new Mock<IMongoCollection<LeaveType>>();
        _mockNotificationService = new Mock<INotificationService>();

        _mockContext.Setup(c => c.LeaveRequests).Returns(_mockLeaveRequests.Object);
        _mockContext.Setup(c => c.LeaveBalances).Returns(_mockLeaveBalances.Object);
        _mockContext.Setup(c => c.Users).Returns(_mockUsers.Object);
        _mockContext.Setup(c => c.LeaveTypes).Returns(_mockLeaveTypes.Object);

        _service = new LeaveService(_mockContext.Object, _mockHubContext.Object, _mockNotificationService.Object);
    }

    [Fact]
    public async Task ApproveAsync_ShouldReturnNull_WhenRequestNotFound()
    {
        // Arrange
        var id = "invalid-id";
        var dto = new LeaveRequestUpdateDto { Comment = "Approved", ApproverId = "admin" };

        _mockLeaveRequests.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<LeaveRequest>>(),
            It.IsAny<UpdateDefinition<LeaveRequest>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        var result = await _service.ApproveAsync(id, dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveAsync_ShouldSucceed_AndNotifyUser()
    {
        // Arrange
        var id = "req-1";
        var employeeId = "emp-123";
        var leaveTypeId = "type-annual";
        var dto = new LeaveRequestUpdateDto { Comment = "Have fun!", ApproverId = "admin" };

        var request = new LeaveRequest
        {
            Id = id,
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            TotalDays = 3,
            Status = "Pending"
        };

        var balance = new LeaveBalance
        {
            Id = "bal-1",
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            Year = DateTime.UtcNow.Year,
            RemainingDays = 10,
            UsedDays = 2
        };

        // 1. Mock UpdateOneAsync for LeaveRequests
        _mockLeaveRequests.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<LeaveRequest>>(),
            It.IsAny<UpdateDefinition<LeaveRequest>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // 2. Mock Find for the request (needed for balance update and mapping)
        SetupMockCursor(_mockLeaveRequests, new List<LeaveRequest> { request });

        // 3. Mock Find for the balance
        SetupMockCursor(_mockLeaveBalances, new List<LeaveBalance> { balance });

        // 4. Mock UpdateOneAsync for LeaveBalances
        _mockLeaveBalances.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<LeaveBalance>>(),
            It.IsAny<UpdateDefinition<LeaveBalance>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // 5. Mock User and LeaveType for MapToDto
        SetupMockCursor(_mockUsers, new List<User> { new User { Id = employeeId, Username = "TestUser" } });
        SetupMockCursor(_mockLeaveTypes, new List<LeaveType> { new LeaveType { Id = leaveTypeId, TypeName = "Annual" } });

        // 6. Mock SignalR
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(employeeId)).Returns(mockClientProxy.Object);

        // Act
        var result = await _service.ApproveAsync(id, dto);

        // Assert - Verify the correct methods were called
        _mockLeaveRequests.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<LeaveRequest>>(),
            It.IsAny<UpdateDefinition<LeaveRequest>>(),
            null,
            default), Times.Once);
        _mockLeaveBalances.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<LeaveBalance>>(),
            It.IsAny<UpdateDefinition<LeaveBalance>>(),
            null,
            default), Times.Once);
    }

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
