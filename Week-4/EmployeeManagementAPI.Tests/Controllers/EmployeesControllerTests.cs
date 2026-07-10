using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Week3_EmployeeManagementAPI.Controllers;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Responses;

namespace EmployeeManagementAPI.Tests.Controllers
{
    /// <summary>
    /// Unit tests for EmployeesController.
    /// Mocks IEmployeeService to isolate controller logic.
    /// Verifies HTTP status codes, response shapes, and routing behavior.
    /// </summary>
    public class EmployeesControllerTests
    {
        private readonly Mock<IEmployeeService> _mockService;
        private readonly Mock<ILogger<EmployeesController>> _mockLogger;
        private readonly EmployeesController _controller;

        public EmployeesControllerTests()
        {
            _mockService = new Mock<IEmployeeService>();
            _mockLogger  = new Mock<ILogger<EmployeesController>>();
            _controller  = new EmployeesController(_mockService.Object, _mockLogger.Object);
        }

        private static EmployeeReadDto CreateSampleDto(int id = 1)
        {
            return new EmployeeReadDto
            {
                EmployeeId     = id,
                FirstName      = "John",
                LastName       = "Doe",
                FullName       = "John Doe",
                Email          = "john.doe@company.com",
                Phone          = "555-0100",
                Position       = "Developer",
                Salary         = 75000m,
                HireDate       = new DateTime(2024, 1, 15),
                IsActive       = true,
                DepartmentId   = 1,
                DepartmentName = "Engineering"
            };
        }

        // ───────────────────────────────────────────────────
        // GET All Employees
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllEmployees_ShouldReturn200WithEmployeeList()
        {
            // Arrange
            var employees = new List<EmployeeReadDto> { CreateSampleDto(1), CreateSampleDto(2) };
            _mockService.Setup(s => s.GetAllEmployeesAsync(It.IsAny<EmployeeQueryParameters>()))
                        .ReturnsAsync(employees);

            // Act
            var result = await _controller.GetAllEmployees(new EmployeeQueryParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var response = Assert.IsType<ApiResponse<List<EmployeeReadDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count);
        }

        [Fact]
        public async Task GetAllEmployees_EmptyList_ShouldReturn200WithEmptyList()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllEmployeesAsync(It.IsAny<EmployeeQueryParameters>()))
                        .ReturnsAsync(new List<EmployeeReadDto>());

            // Act
            var result = await _controller.GetAllEmployees(new EmployeeQueryParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<List<EmployeeReadDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Empty(response.Data!);
        }

        // ───────────────────────────────────────────────────
        // GET Employee By ID
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task GetEmployeeById_ExistingId_ShouldReturn200()
        {
            // Arrange
            var dto = CreateSampleDto(1);
            _mockService.Setup(s => s.GetEmployeeByIdAsync(1)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetEmployeeById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var response = Assert.IsType<ApiResponse<EmployeeReadDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(1, response.Data!.EmployeeId);
        }

        [Fact]
        public async Task GetEmployeeById_NonExistingId_ShouldReturn404()
        {
            // Arrange
            _mockService.Setup(s => s.GetEmployeeByIdAsync(9999)).ReturnsAsync((EmployeeReadDto?)null);

            // Act
            var result = await _controller.GetEmployeeById(9999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);

            var response = Assert.IsType<ApiResponse<EmployeeReadDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Contains("9999", response.Message);
        }

        // ───────────────────────────────────────────────────
        // POST Create Employee
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateEmployee_ValidDto_ShouldReturn201()
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

            var createdDto = new EmployeeReadDto
            {
                EmployeeId     = 10,
                FirstName      = "Alice",
                LastName       = "Wonder",
                FullName       = "Alice Wonder",
                Email          = "alice@company.com",
                Position       = "Designer",
                Salary         = 65000m,
                HireDate       = DateTime.Now,
                IsActive       = true,
                DepartmentId   = 1,
                DepartmentName = "Engineering"
            };

            _mockService.Setup(s => s.CreateEmployeeAsync(createDto)).ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateEmployee(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);

            var response = Assert.IsType<ApiResponse<EmployeeReadDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal(10, response.Data!.EmployeeId);
            Assert.Equal("Alice", response.Data.FirstName);
        }

        // ───────────────────────────────────────────────────
        // PUT Update Employee
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task UpdateEmployee_ExistingId_ShouldReturn200()
        {
            // Arrange
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

            var updatedReadDto = new EmployeeReadDto
            {
                EmployeeId     = 1,
                FirstName      = "Updated",
                LastName       = "Name",
                FullName       = "Updated Name",
                Email          = "updated@company.com",
                Position       = "Senior Dev",
                Salary         = 95000m,
                DepartmentId   = 1,
                DepartmentName = "Engineering"
            };

            _mockService.Setup(s => s.UpdateEmployeeAsync(1, updateDto)).ReturnsAsync(updatedReadDto);

            // Act
            var result = await _controller.UpdateEmployee(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<EmployeeReadDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Updated", response.Data!.FirstName);
        }

        [Fact]
        public async Task UpdateEmployee_IdMismatch_ShouldReturn400()
        {
            // Arrange
            var updateDto = new EmployeeUpdateDto
            {
                EmployeeId   = 5,   // doesn't match route id=1
                FirstName    = "Mismatch",
                LastName     = "User",
                Email        = "mis@company.com",
                Position     = "Dev",
                Salary       = 50000m,
                HireDate     = DateTime.Now,
                DepartmentId = 1
            };

            // Act
            var result = await _controller.UpdateEmployee(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task UpdateEmployee_NonExistingId_ShouldReturn404()
        {
            // Arrange
            var updateDto = new EmployeeUpdateDto
            {
                EmployeeId   = 9999,
                FirstName    = "Ghost",
                LastName     = "User",
                Email        = "ghost@company.com",
                Position     = "None",
                Salary       = 0m,
                HireDate     = DateTime.Now,
                DepartmentId = 1
            };

            _mockService.Setup(s => s.UpdateEmployeeAsync(9999, updateDto)).ReturnsAsync((EmployeeReadDto?)null);

            // Act
            var result = await _controller.UpdateEmployee(9999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        // ───────────────────────────────────────────────────
        // DELETE Employee
        // ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteEmployee_ExistingId_ShouldReturn200()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteEmployeeAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteEmployee(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task DeleteEmployee_NonExistingId_ShouldReturn404()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteEmployeeAsync(9999)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteEmployee(9999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }
    }
}
