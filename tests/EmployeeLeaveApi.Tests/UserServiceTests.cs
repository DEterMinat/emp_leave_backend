using Moq;
using MongoDB.Driver;
using EmployeeLeaveApi.Services;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Models;
using EmployeeLeaveApi.DTOs;

namespace EmployeeLeaveApi.Tests;

public class UserServiceTests
{
    private readonly Mock<IMongoDbContext> _mockContext;
    private readonly Mock<IMongoCollection<User>> _mockUsers;
    private readonly Mock<IMongoCollection<Role>> _mockRoles;
    private readonly Mock<IMongoCollection<Department>> _mockDepartments;
    private readonly Mock<IMongoCollection<Employee>> _mockEmployees;
    private readonly Mock<IMongoCollection<LeaveType>> _mockLeaveTypes;
    private readonly Mock<IMongoCollection<LeaveBalance>> _mockLeaveBalances;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockContext = new Mock<IMongoDbContext>();
        _mockUsers = new Mock<IMongoCollection<User>>();
        _mockRoles = new Mock<IMongoCollection<Role>>();
        _mockDepartments = new Mock<IMongoCollection<Department>>();
        _mockEmployees = new Mock<IMongoCollection<Employee>>();
        _mockLeaveTypes = new Mock<IMongoCollection<LeaveType>>();
        _mockLeaveBalances = new Mock<IMongoCollection<LeaveBalance>>();

        _mockContext.Setup(c => c.Users).Returns(_mockUsers.Object);
        _mockContext.Setup(c => c.Roles).Returns(_mockRoles.Object);
        _mockContext.Setup(c => c.Departments).Returns(_mockDepartments.Object);
        _mockContext.Setup(c => c.Employees).Returns(_mockEmployees.Object);
        _mockContext.Setup(c => c.LeaveTypes).Returns(_mockLeaveTypes.Object);
        _mockContext.Setup(c => c.LeaveBalances).Returns(_mockLeaveBalances.Object);

        _service = new UserService(_mockContext.Object);
    }

    // ==================== GetByIdAsync ====================

    [Fact]
    public async Task GetByIdAsync_UserNotFound_ShouldReturnNull()
    {
        // Arrange
        SetupMockCursor(_mockUsers, new List<User>());

        // Act
        var result = await _service.GetByIdAsync("nonexistent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_UserFound_ShouldReturnDto()
    {
        // Arrange
        var user = new User
        {
            Id = "user-1",
            Username = "john.doe",
            RoleId = "role-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            CreatedAt = DateTime.UtcNow
        };

        var role = new Role { Id = "role-1", RoleName = "Employee" };

        SetupMockCursor(_mockUsers, new List<User> { user });
        SetupMockCursor(_mockRoles, new List<Role> { role });

        // Act
        var result = await _service.GetByIdAsync("user-1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-1", result.Id);
        Assert.Equal("john.doe", result.Username);
        Assert.Equal("Employee", result.RoleName);
        Assert.Equal("John", result.FirstName);
    }

    // ==================== GetAllAsync ====================

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        SetupMockCursor(_mockUsers, new List<User>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithUsers_ShouldReturnAllDtos()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "user-1", Username = "john", RoleId = "role-1", CreatedAt = DateTime.UtcNow },
            new User { Id = "user-2", Username = "jane", RoleId = "role-1", CreatedAt = DateTime.UtcNow }
        };

        SetupMockCursor(_mockUsers, users);
        SetupMockCursor(_mockRoles, new List<Role> { new Role { Id = "role-1", RoleName = "Employee" } });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    // ==================== GetByUsernameAsync ====================

    [Fact]
    public async Task GetByUsernameAsync_NotFound_ShouldReturnNull()
    {
        // Arrange
        SetupMockCursor(_mockUsers, new List<User>());

        // Act
        var result = await _service.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_Found_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            Id = "user-1",
            Username = "john.doe",
            RoleId = "role-1",
            CreatedAt = DateTime.UtcNow
        };

        SetupMockCursor(_mockUsers, new List<User> { user });

        // Act
        var result = await _service.GetByUsernameAsync("john.doe");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("john.doe", result.Username);
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_UserExists_ShouldReturnTrue()
    {
        // Arrange
        _mockUsers.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            default))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        // Act
        var result = await _service.DeleteAsync("user-1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_UserNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockUsers.Setup(c => c.DeleteOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            default))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        // Act
        var result = await _service.DeleteAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    // ==================== UpdateAsync ====================

    [Fact]
    public async Task UpdateAsync_UserNotFound_ShouldReturnNull()
    {
        // Arrange
        var dto = new UserUpdateDto { FirstName = "Updated" };

        _mockUsers.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        var result = await _service.UpdateAsync("nonexistent", dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UserFound_ShouldReturnUpdatedDto()
    {
        // Arrange
        var dto = new UserUpdateDto { FirstName = "Updated", Email = "updated@test.com" };

        _mockUsers.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<User>>(),
            It.IsAny<UpdateDefinition<User>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Mock Employee update
        _mockEmployees.Setup(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<Employee>>(),
            It.IsAny<UpdateDefinition<Employee>>(),
            null,
            default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Mock GetByIdAsync path
        var updatedUser = new User
        {
            Id = "user-1",
            Username = "john.doe",
            RoleId = "role-1",
            FirstName = "Updated",
            Email = "updated@test.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        SetupMockCursor(_mockUsers, new List<User> { updatedUser });
        SetupMockCursor(_mockRoles, new List<Role> { new Role { Id = "role-1", RoleName = "Employee" } });

        // Act
        var result = await _service.UpdateAsync("user-1", dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.FirstName);
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
