using Moq;
using MongoDB.Driver;
using EmployeeLeaveApi.Services;
using EmployeeLeaveApi.Data;
using EmployeeLeaveApi.Models;
using EmployeeLeaveApi.DTOs;

namespace EmployeeLeaveApi.Tests;

public class AttendanceServiceTests
{
    private readonly Mock<IMongoDbContext> _mockContext;
    private readonly Mock<IMongoCollection<Attendance>> _mockAttendances;
    private readonly Mock<IMongoCollection<Employee>> _mockEmployees;
    private readonly Mock<IMongoCollection<User>> _mockUsers;
    private readonly Mock<IMongoCollection<Department>> _mockDepartments;
    private readonly AttendanceService _service;

    public AttendanceServiceTests()
    {
        _mockContext = new Mock<IMongoDbContext>();
        _mockAttendances = new Mock<IMongoCollection<Attendance>>();
        _mockEmployees = new Mock<IMongoCollection<Employee>>();
        _mockUsers = new Mock<IMongoCollection<User>>();
        _mockDepartments = new Mock<IMongoCollection<Department>>();

        _mockContext.Setup(c => c.Attendances).Returns(_mockAttendances.Object);
        _mockContext.Setup(c => c.Employees).Returns(_mockEmployees.Object);
        _mockContext.Setup(c => c.Users).Returns(_mockUsers.Object);
        _mockContext.Setup(c => c.Departments).Returns(_mockDepartments.Object);

        _service = new AttendanceService(_mockContext.Object);
    }

    // ==================== CheckInAsync ====================

    [Fact]
    public async Task CheckInAsync_WithValidEmployee_ShouldSucceed()
    {
        // Arrange
        var employeeId = "emp-1";
        var dto = new CheckInDto { EmployeeID = employeeId, Notes = "Working from office" };

        var employee = new Employee
        {
            Id = employeeId,
            UserId = "user-1",
            DepartmentId = "dept-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        // Mock: employee lookup returns found
        SetupMockCursor(_mockEmployees, new List<Employee> { employee });
        // Mock: no existing attendance today
        SetupMockCursor(_mockAttendances, new List<Attendance>());
        // Mock: InsertOneAsync
        _mockAttendances.Setup(c => c.InsertOneAsync(
            It.IsAny<Attendance>(), null, default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CheckInAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeId, result.EmployeeID);
        Assert.NotNull(result.CheckInTime);
    }

    [Fact]
    public async Task CheckInAsync_AlreadyCheckedIn_ShouldThrow()
    {
        // Arrange
        var employeeId = "emp-1";
        var dto = new CheckInDto { EmployeeID = employeeId };

        var employee = new Employee
        {
            Id = employeeId,
            UserId = "user-1",
            DepartmentId = "dept-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        var existingAttendance = new Attendance
        {
            AttendanceID = "att-1",
            EmployeeID = employeeId,
            AttendanceDate = DateTime.UtcNow.Date,
            CheckInTime = DateTime.UtcNow.AddHours(-2)
        };

        // Mock: employee found
        SetupMockCursor(_mockEmployees, new List<Employee> { employee });
        // Mock: existing attendance found
        SetupMockCursor(_mockAttendances, new List<Attendance> { existingAttendance });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CheckInAsync(dto));
    }

    [Fact]
    public async Task CheckInAsync_EmployeeNotFound_ShouldThrow()
    {
        // Arrange
        var dto = new CheckInDto { EmployeeID = "nonexistent" };

        // Mock: no employee found
        SetupMockCursor(_mockEmployees, new List<Employee>());
        // Mock: no user found for auto-create path
        SetupMockCursor(_mockUsers, new List<User>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CheckInAsync(dto));
    }

    // ==================== CheckOutAsync ====================

    [Fact]
    public async Task CheckOutAsync_NotCheckedIn_ShouldThrow()
    {
        // Arrange
        var employeeId = "emp-1";
        var dto = new CheckOutDto { EmployeeID = employeeId };

        var employee = new Employee
        {
            Id = employeeId,
            UserId = "user-1",
            DepartmentId = "dept-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        // Mock: employee found
        SetupMockCursor(_mockEmployees, new List<Employee> { employee });
        // Mock: no attendance today
        SetupMockCursor(_mockAttendances, new List<Attendance>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CheckOutAsync(dto));
    }

    [Fact]
    public async Task CheckOutAsync_AlreadyCheckedOut_ShouldThrow()
    {
        // Arrange
        var employeeId = "emp-1";
        var dto = new CheckOutDto { EmployeeID = employeeId };

        var employee = new Employee
        {
            Id = employeeId,
            UserId = "user-1",
            DepartmentId = "dept-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        var existingAttendance = new Attendance
        {
            AttendanceID = "att-1",
            EmployeeID = employeeId,
            AttendanceDate = DateTime.UtcNow.Date,
            CheckInTime = DateTime.UtcNow.AddHours(-4),
            CheckOutTime = DateTime.UtcNow.AddHours(-1) // Already checked out
        };

        // Mock: employee found
        SetupMockCursor(_mockEmployees, new List<Employee> { employee });
        // Mock: existing attendance with checkout
        SetupMockCursor(_mockAttendances, new List<Attendance> { existingAttendance });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CheckOutAsync(dto));
    }

    [Fact]
    public async Task CheckOutAsync_EmployeeNotFound_ShouldThrow()
    {
        // Arrange
        var dto = new CheckOutDto { EmployeeID = "nonexistent" };

        // Mock: no employee found
        SetupMockCursor(_mockEmployees, new List<Employee>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CheckOutAsync(dto));
    }

    // ==================== GetTodayAttendanceAsync ====================

    [Fact]
    public async Task GetTodayAttendanceAsync_NoRecord_ShouldReturnNull()
    {
        // Arrange
        var employeeId = "emp-1";
        var employee = new Employee
        {
            Id = employeeId,
            UserId = "user-1",
            DepartmentId = "dept-1",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com"
        };

        SetupMockCursor(_mockEmployees, new List<Employee> { employee });
        SetupMockCursor(_mockAttendances, new List<Attendance>());

        // Act
        var result = await _service.GetTodayAttendanceAsync(employeeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTodayAttendanceAsync_InvalidEmployee_ShouldReturnNull()
    {
        // Arrange
        SetupMockCursor(_mockEmployees, new List<Employee>());

        // Act
        var result = await _service.GetTodayAttendanceAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    // ==================== GetHistoryByEmployeeIdAsync ====================

    [Fact]
    public async Task GetHistoryByEmployeeIdAsync_InvalidEmployee_ShouldReturnEmptyList()
    {
        // Arrange
        SetupMockCursor(_mockEmployees, new List<Employee>());

        // Act
        var result = await _service.GetHistoryByEmployeeIdAsync("nonexistent");

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
