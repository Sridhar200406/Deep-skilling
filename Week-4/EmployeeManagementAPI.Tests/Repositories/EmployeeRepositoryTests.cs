using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Models;
using Week3_EmployeeManagementAPI.Repositories;

namespace EmployeeManagementAPI.Tests.Repositories
{
    /// <summary>
    /// Unit tests for EmployeeRepository using EF Core InMemory database.
    /// Each test creates its own DbContext to ensure test isolation.
    /// </summary>
    public class EmployeeRepositoryTests
    {
        private static AppDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new AppDbContext(options);

            // Seed a department for FK references
            if (!context.Departments.Any())
            {
                context.Departments.Add(new Department
                {
                    DepartmentId = 1,
                    DepartmentName = "Engineering",
                    Description = "Software Engineering"
                });
                context.SaveChanges();
            }

            return context;
        }

        private static Employee CreateSampleEmployee(int id = 0, string firstName = "John", string lastName = "Doe")
        {
            return new Employee
            {
                EmployeeId  = id,
                FirstName   = firstName,
                LastName    = lastName,
                Email       = $"{firstName.ToLower()}.{lastName.ToLower()}@company.com",
                Phone       = "555-0100",
                Position    = "Developer",
                Salary      = 75000m,
                HireDate    = new DateTime(2024, 1, 15),
                IsActive    = true,
                DepartmentId = 1
            };
        }

        // ───────────────────────────────────────────────────
        // AddAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task AddAsync_ShouldAddEmployeeToDatabase()
        {
            // Arrange
            using var context = CreateContext(nameof(AddAsync_ShouldAddEmployeeToDatabase));
            var repo = new EmployeeRepository(context);
            var employee = CreateSampleEmployee();

            // Act
            await repo.AddAsync(employee);

            // Assert
            var savedEmployee = await context.Employees.FirstOrDefaultAsync();
            Assert.NotNull(savedEmployee);
            Assert.Equal("John", savedEmployee.FirstName);
            Assert.Equal("Doe", savedEmployee.LastName);
        }

        [Fact]
        public async Task AddAsync_ShouldAutoGenerateEmployeeId()
        {
            // Arrange
            using var context = CreateContext(nameof(AddAsync_ShouldAutoGenerateEmployeeId));
            var repo = new EmployeeRepository(context);
            var employee = CreateSampleEmployee();

            // Act
            await repo.AddAsync(employee);

            // Assert
            Assert.True(employee.EmployeeId > 0);
        }

        // ───────────────────────────────────────────────────
        // GetByIdAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnEmployee()
        {
            // Arrange
            using var context = CreateContext(nameof(GetByIdAsync_ExistingId_ShouldReturnEmployee));
            var repo = new EmployeeRepository(context);
            var employee = CreateSampleEmployee();
            await repo.AddAsync(employee);

            // Act
            var result = await repo.GetByIdAsync(employee.EmployeeId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(employee.EmployeeId, result.EmployeeId);
            Assert.Equal("John", result.FirstName);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            using var context = CreateContext(nameof(GetByIdAsync_NonExistingId_ShouldReturnNull));
            var repo = new EmployeeRepository(context);

            // Act
            var result = await repo.GetByIdAsync(9999);

            // Assert
            Assert.Null(result);
        }

        // ───────────────────────────────────────────────────
        // GetAllAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEmployees()
        {
            // Arrange
            using var context = CreateContext(nameof(GetAllAsync_ShouldReturnAllEmployees));
            var repo = new EmployeeRepository(context);
            await repo.AddAsync(CreateSampleEmployee(firstName: "Alice", lastName: "Smith"));
            await repo.AddAsync(CreateSampleEmployee(firstName: "Bob", lastName: "Jones"));

            // Act
            var result = await repo.GetAllAsync(new EmployeeQueryParameters());

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_ShouldFilterResults()
        {
            // Arrange
            using var context = CreateContext(nameof(GetAllAsync_WithSearchTerm_ShouldFilterResults));
            var repo = new EmployeeRepository(context);
            await repo.AddAsync(CreateSampleEmployee(firstName: "Alice", lastName: "Smith"));
            await repo.AddAsync(CreateSampleEmployee(firstName: "Bob", lastName: "Jones"));

            var queryParams = new EmployeeQueryParameters { SearchTerm = "Alice" };

            // Act
            var result = await repo.GetAllAsync(queryParams);

            // Assert
            Assert.Single(result);
            Assert.Equal("Alice", result[0].FirstName);
        }

        [Fact]
        public async Task GetAllAsync_WithPagination_ShouldReturnPagedResults()
        {
            // Arrange
            using var context = CreateContext(nameof(GetAllAsync_WithPagination_ShouldReturnPagedResults));
            var repo = new EmployeeRepository(context);
            for (int i = 1; i <= 15; i++)
            {
                await repo.AddAsync(CreateSampleEmployee(firstName: $"Emp{i}", lastName: $"Last{i}"));
            }

            var queryParams = new EmployeeQueryParameters { PageNumber = 2, PageSize = 5 };

            // Act
            var result = await repo.GetAllAsync(queryParams);

            // Assert
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_WithDepartmentFilter_ShouldFilterByDepartment()
        {
            // Arrange
            using var context = CreateContext(nameof(GetAllAsync_WithDepartmentFilter_ShouldFilterByDepartment));
            var repo = new EmployeeRepository(context);
            await repo.AddAsync(CreateSampleEmployee(firstName: "Alice"));
            var queryParams = new EmployeeQueryParameters { DepartmentId = 1 };

            // Act
            var result = await repo.GetAllAsync(queryParams);

            // Assert
            Assert.All(result, e => Assert.Equal(1, e.DepartmentId));
        }

        // ───────────────────────────────────────────────────
        // UpdateAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEmployeeProperties()
        {
            // Arrange
            using var context = CreateContext(nameof(UpdateAsync_ShouldUpdateEmployeeProperties));
            var repo = new EmployeeRepository(context);
            var employee = CreateSampleEmployee();
            await repo.AddAsync(employee);

            // Act
            employee.FirstName = "Jane";
            employee.Salary = 90000m;
            await repo.UpdateAsync(employee);

            // Assert
            var updated = await context.Employees.FindAsync(employee.EmployeeId);
            Assert.NotNull(updated);
            Assert.Equal("Jane", updated.FirstName);
            Assert.Equal(90000m, updated.Salary);
        }

        // ───────────────────────────────────────────────────
        // DeleteAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ShouldRemoveEmployeeFromDatabase()
        {
            // Arrange
            using var context = CreateContext(nameof(DeleteAsync_ShouldRemoveEmployeeFromDatabase));
            var repo = new EmployeeRepository(context);
            var employee = CreateSampleEmployee();
            await repo.AddAsync(employee);

            // Act
            await repo.DeleteAsync(employee);

            // Assert
            var deleted = await context.Employees.FindAsync(employee.EmployeeId);
            Assert.Null(deleted);
        }

        // ───────────────────────────────────────────────────
        // ExistsAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task ExistsAsync_ExistingId_ShouldReturnTrue()
        {
            // Arrange
            using var context = CreateContext(nameof(ExistsAsync_ExistingId_ShouldReturnTrue));
            var repo = new EmployeeRepository(context);
            var employee = CreateSampleEmployee();
            await repo.AddAsync(employee);

            // Act
            var exists = await repo.ExistsAsync(employee.EmployeeId);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_NonExistingId_ShouldReturnFalse()
        {
            // Arrange
            using var context = CreateContext(nameof(ExistsAsync_NonExistingId_ShouldReturnFalse));
            var repo = new EmployeeRepository(context);

            // Act
            var exists = await repo.ExistsAsync(9999);

            // Assert
            Assert.False(exists);
        }
    }
}
