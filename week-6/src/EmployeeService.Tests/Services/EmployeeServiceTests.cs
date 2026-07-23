using EmployeeService.Application.DTOs;
using EmployeeService.Application.Services;
using EmployeeService.Domain.Entities;
using EmployeeService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeService.Tests.Services;

/// <summary>
/// Unit tests for EmployeeAppService using EF Core InMemory database.
/// These run in CI/CD without requiring a real SQL Server.
/// </summary>
public class EmployeeServiceTests : IDisposable
{
    private readonly EmployeeDbContext _context;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<EmployeeAppService>> _loggerMock;
    private readonly IEmployeeAppService _service;

    public EmployeeServiceTests()
    {
        // Use InMemory database — no SQL Server needed in CI
        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique per test class
            .Options;

        _context = new EmployeeDbContext(options);
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<EmployeeAppService>>();

        // Cache always returns null (cache miss) for unit tests
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((byte[]?)null);

        _service = new EmployeeAppService(_context, _cacheMock.Object, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Departments.AddRange(
            new Department { Id = 1, Name = "Engineering", Description = "Engineering Dept", IsActive = true },
            new Department { Id = 2, Name = "HR", Description = "Human Resources", IsActive = true }
        );
        _context.SaveChanges();
    }

    // ─── CREATE ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidEmployee_ReturnsCreatedEmployee()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Position = "Developer",
            Salary = 75000,
            DepartmentId = 1,
            HireDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@test.com");
        result.DepartmentId.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "duplicate@test.com",
            Position = "Manager",
            Salary = 90000,
            DepartmentId = 1,
            HireDate = DateTime.UtcNow
        };
        await _service.CreateAsync(dto);

        // Act & Assert
        var duplicateDto = dto with { FirstName = "Another" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(duplicateDto));
    }

    [Fact]
    public async Task CreateAsync_InvalidDepartment_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateEmployeeDto
        {
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@test.com",
            Position = "Analyst",
            Salary = 60000,
            DepartmentId = 999, // non-existent department
            HireDate = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    // ─── READ ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingEmployee_ReturnsEmployee()
    {
        // Arrange
        var created = await _service.CreateAsync(new CreateEmployeeDto
        {
            FirstName = "Alice",
            LastName = "Green",
            Email = "alice@test.com",
            Position = "Lead",
            Salary = 85000,
            DepartmentId = 2,
            HireDate = DateTime.UtcNow
        });

        // Act
        var result = await _service.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentEmployee_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(99999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_ReturnsFilteredResults()
    {
        // Arrange
        await _service.CreateAsync(new CreateEmployeeDto
        {
            FirstName = "Charlie",
            LastName = "Brown",
            Email = "charlie@test.com",
            Position = "Designer",
            Salary = 65000,
            DepartmentId = 1,
            HireDate = DateTime.UtcNow
        });
        await _service.CreateAsync(new CreateEmployeeDto
        {
            FirstName = "Diana",
            LastName = "Prince",
            Email = "diana@test.com",
            Position = "Architect",
            Salary = 95000,
            DepartmentId = 1,
            HireDate = DateTime.UtcNow
        });

        // Act
        var result = await _service.GetAllAsync(1, 10, "charlie");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().FullName.Should().Contain("Charlie");
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange — create 5 employees
        for (int i = 1; i <= 5; i++)
        {
            await _service.CreateAsync(new CreateEmployeeDto
            {
                FirstName = $"Emp{i}",
                LastName = "Test",
                Email = $"emp{i}@test.com",
                Position = "Role",
                Salary = 50000,
                DepartmentId = 1,
                HireDate = DateTime.UtcNow
            });
        }

        // Act — page 1, size 2
        var page1 = await _service.GetAllAsync(1, 2, null);
        // Act — page 2, size 2
        var page2 = await _service.GetAllAsync(2, 2, null);

        // Assert
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.TotalPages.Should().Be(3);
        page1.HasNextPage.Should().BeTrue();
        page2.HasPreviousPage.Should().BeTrue();
    }

    // ─── UPDATE ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingEmployee_ReturnsUpdatedEmployee()
    {
        // Arrange
        var created = await _service.CreateAsync(new CreateEmployeeDto
        {
            FirstName = "Eve",
            LastName = "Wilson",
            Email = "eve@test.com",
            Position = "Junior Dev",
            Salary = 55000,
            DepartmentId = 1,
            HireDate = DateTime.UtcNow
        });

        var updateDto = new UpdateEmployeeDto
        {
            FirstName = "Eve",
            LastName = "Wilson",
            Email = "eve@test.com",
            Position = "Senior Dev",   // Changed
            Salary = 80000,            // Changed
            IsActive = true,
            DepartmentId = 1
        };

        // Act
        var result = await _service.UpdateAsync(created.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Position.Should().Be("Senior Dev");
        result.Salary.Should().Be(80000);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentEmployee_ReturnsNull()
    {
        var result = await _service.UpdateAsync(99999, new UpdateEmployeeDto
        {
            FirstName = "X", LastName = "Y", Email = "x@y.com",
            Position = "Role", Salary = 0, IsActive = true, DepartmentId = 1
        });
        result.Should().BeNull();
    }

    // ─── DELETE ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEmployee_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var created = await _service.CreateAsync(new CreateEmployeeDto
        {
            FirstName = "Frank",
            LastName = "Castle",
            Email = "frank@test.com",
            Position = "Specialist",
            Salary = 70000,
            DepartmentId = 2,
            HireDate = DateTime.UtcNow
        });

        // Act
        var deleted = await _service.DeleteAsync(created.Id);

        // Assert — returns true
        deleted.Should().BeTrue();

        // Assert — employee is soft-deleted (not accessible via GetById)
        var afterDelete = await _service.GetByIdAsync(created.Id);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentEmployee_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(99999);
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
