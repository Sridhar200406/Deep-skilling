using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Mapping;
using Week3_EmployeeManagementAPI.Models;
using Week3_EmployeeManagementAPI.Services;

namespace EmployeeManagementAPI.Tests.Services
{
    /// <summary>
    /// Unit tests for EmployeeService using Moq to mock the repository layer.
    /// Verifies business logic, caching behavior, and error handling.
    /// </summary>
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<EmployeeService>> _mockLogger;
        private readonly EmployeeService _service;

        public EmployeeServiceTests()
        {
            _mockRepo   = new Mock<IEmployeeRepository>();
            _mockLogger = new Mock<ILogger<EmployeeService>>();
            _cache      = new MemoryCache(new MemoryCacheOptions());

            // Configure AutoMapper with the real profile
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EmployeeProfile>();
            });
            _mapper = mapperConfig.CreateMapper();

            _service = new EmployeeService(_mockRepo.Object, _mapper, _cache, _mockLogger.Object);
        }

        private static Employee CreateSampleEmployee(int id = 1)
        {
            return new Employee
            {
                EmployeeId   = id,
                FirstName    = "John",
                LastName     = "Doe",
                Email        = "john.doe@company.com",
                Phone        = "555-0100",
                Position     = "Developer",
                Salary       = 75000m,
                HireDate     = new DateTime(2024, 1, 15),
                IsActive     = true,
                DepartmentId = 1,
                Department   = new Department { DepartmentId = 1, DepartmentName = "Engineering" }
            };
        }

        // ───────────────────────────────────────────────────
        // GetAllEmployeesAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllEmployeesAsync_ShouldReturnMappedDtoList()
        {
            // Arrange
            var employees = new List<Employee>
            {
                CreateSampleEmployee(1),
                CreateSampleEmployee(2)
            };
            employees[1].FirstName = "Jane";
            employees[1].Email = "jane.doe@company.com";

            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<EmployeeQueryParameters>()))
                     .ReturnsAsync(employees);

            // Act
            var result = await _service.GetAllEmployeesAsync(new EmployeeQueryParameters());

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("John", result[0].FirstName);
            Assert.Equal("Jane", result[1].FirstName);
            Assert.Equal("Engineering", result[0].DepartmentName);
        }

        [Fact]
        public async Task GetAllEmployeesAsync_ShouldCacheResults()
        {
            // Arrange
            var employees = new List<Employee> { CreateSampleEmployee() };
            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<EmployeeQueryParameters>()))
                     .ReturnsAsync(employees);

            var queryParams = new EmployeeQueryParameters();

            // Act
            await _service.GetAllEmployeesAsync(queryParams); // cache miss
            await _service.GetAllEmployeesAsync(queryParams); // cache hit

            // Assert: repository called only once due to caching
            _mockRepo.Verify(r => r.GetAllAsync(It.IsAny<EmployeeQueryParameters>()), Times.Once);
        }

        [Fact]
        public async Task GetAllEmployeesAsync_EmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<EmployeeQueryParameters>()))
                     .ReturnsAsync(new List<Employee>());

            // Act
            var result = await _service.GetAllEmployeesAsync(new EmployeeQueryParameters());

            // Assert
            Assert.Empty(result);
        }

        // ───────────────────────────────────────────────────
        // GetEmployeeByIdAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task GetEmployeeByIdAsync_ExistingId_ShouldReturnDto()
        {
            // Arrange
            var employee = CreateSampleEmployee(1);
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);

            // Act
            var result = await _service.GetEmployeeByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EmployeeId);
            Assert.Equal("John Doe", result.FullName);
            Assert.Equal("Engineering", result.DepartmentName);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(9999)).ReturnsAsync((Employee?)null);

            // Act
            var result = await _service.GetEmployeeByIdAsync(9999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_ShouldCacheResult()
        {
            // Arrange
            var employee = CreateSampleEmployee(1);
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);

            // Act
            await _service.GetEmployeeByIdAsync(1); // cache miss
            await _service.GetEmployeeByIdAsync(1); // cache hit

            // Assert
            _mockRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        // ───────────────────────────────────────────────────
        // CreateEmployeeAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateEmployeeAsync_ShouldCallRepoAndReturnDto()
        {
            // Arrange
            var createDto = new EmployeeCreateDto
            {
                FirstName    = "Alice",
                LastName     = "Wonder",
                Email        = "alice@company.com",
                Position     = "Designer",
                Salary       = 65000m,
                HireDate     = DateTime.Now,
                IsActive     = true,
                DepartmentId = 1
            };

            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Employee>()))
                     .Callback<Employee>(e =>
                     {
                         e.EmployeeId = 10;
                         e.Department = new Department { DepartmentId = 1, DepartmentName = "Engineering" };
                     })
                     .Returns(Task.CompletedTask);

            _mockRepo.Setup(r => r.LoadDepartmentAsync(It.IsAny<Employee>()))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateEmployeeAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Alice", result.FirstName);
            Assert.Equal("Wonder", result.LastName);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployeeAsync_ShouldInvalidateCache()
        {
            // Arrange - seed cache first
            var employees = new List<Employee> { CreateSampleEmployee() };
            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<EmployeeQueryParameters>()))
                     .ReturnsAsync(employees);

            await _service.GetAllEmployeesAsync(new EmployeeQueryParameters());

            var createDto = new EmployeeCreateDto
            {
                FirstName    = "New",
                LastName     = "Person",
                Email        = "new@company.com",
                Position     = "Intern",
                Salary       = 40000m,
                HireDate     = DateTime.Now,
                IsActive     = true,
                DepartmentId = 1
            };

            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Employee>()))
                     .Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.LoadDepartmentAsync(It.IsAny<Employee>()))
                     .Returns(Task.CompletedTask);

            // Act
            await _service.CreateEmployeeAsync(createDto);
            await _service.GetAllEmployeesAsync(new EmployeeQueryParameters());

            // Assert: repo.GetAllAsync called twice (cache was invalidated after create)
            _mockRepo.Verify(r => r.GetAllAsync(It.IsAny<EmployeeQueryParameters>()), Times.Exactly(2));
        }

        // ───────────────────────────────────────────────────
        // UpdateEmployeeAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateEmployeeAsync_ExistingId_ShouldReturnUpdatedDto()
        {
            // Arrange
            var existing = CreateSampleEmployee(1);
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Employee>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.LoadDepartmentAsync(It.IsAny<Employee>())).Returns(Task.CompletedTask);

            var updateDto = new EmployeeUpdateDto
            {
                EmployeeId   = 1,
                FirstName    = "Updated",
                LastName     = "Name",
                Email        = "updated@company.com",
                Position     = "Senior Dev",
                Salary       = 95000m,
                HireDate     = DateTime.Now,
                IsActive     = true,
                DepartmentId = 1
            };

            // Act
            var result = await _service.UpdateEmployeeAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated", result!.FirstName);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Employee>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEmployeeAsync_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(9999)).ReturnsAsync((Employee?)null);

            var updateDto = new EmployeeUpdateDto
            {
                EmployeeId = 9999,
                FirstName  = "Ghost",
                LastName   = "User",
                Email      = "ghost@company.com",
                Position   = "None",
                Salary     = 0m,
                HireDate   = DateTime.Now,
                DepartmentId = 1
            };

            // Act
            var result = await _service.UpdateEmployeeAsync(9999, updateDto);

            // Assert
            Assert.Null(result);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Employee>()), Times.Never);
        }

        // ───────────────────────────────────────────────────
        // DeleteEmployeeAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteEmployeeAsync_ExistingId_ShouldReturnTrue()
        {
            // Arrange
            var employee = CreateSampleEmployee(1);
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);
            _mockRepo.Setup(r => r.DeleteAsync(employee)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteEmployeeAsync(1);

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.DeleteAsync(employee), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_NonExistingId_ShouldReturnFalse()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(9999)).ReturnsAsync((Employee?)null);

            // Act
            var result = await _service.DeleteEmployeeAsync(9999);

            // Assert
            Assert.False(result);
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Employee>()), Times.Never);
        }

        // ───────────────────────────────────────────────────
        // EmployeeExistsAsync
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task EmployeeExistsAsync_ShouldDelegateToRepository()
        {
            // Arrange
            _mockRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.ExistsAsync(9999)).ReturnsAsync(false);

            // Act & Assert
            Assert.True(await _service.EmployeeExistsAsync(1));
            Assert.False(await _service.EmployeeExistsAsync(9999));
        }
    }
}
