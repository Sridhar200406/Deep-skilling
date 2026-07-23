using EmployeeService.Application.DTOs;
using EmployeeService.Application.Services;
using EmployeeService.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace EmployeeService.Tests.Controllers;

/// <summary>
/// Unit tests for EmployeesController — mocks the service layer completely.
/// </summary>
public class EmployeesControllerTests
{
    private readonly Mock<IEmployeeAppService> _serviceMock;
    private readonly Mock<ILogger<EmployeesController>> _loggerMock;
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        _serviceMock = new Mock<IEmployeeAppService>();
        _loggerMock = new Mock<ILogger<EmployeesController>>();
        _controller = new EmployeesController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_ExistingEmployee_Returns200WithEmployee()
    {
        // Arrange
        var employee = new EmployeeDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Position = "Developer",
            DepartmentId = 1,
            DepartmentName = "Engineering",
            IsActive = true
        };
        _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(employee);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<EmployeeDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task GetById_NonExistentEmployee_Returns404()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((EmployeeDto?)null);

        var result = await _controller.GetById(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingEmployee_Returns200()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_NonExistentEmployee_Returns404()
    {
        _serviceMock.Setup(s => s.DeleteAsync(99)).ReturnsAsync(false);

        var result = await _controller.Delete(99);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAll_Returns200WithPagedResult()
    {
        var pagedResult = new PagedResult<EmployeeListDto>
        {
            Items = new List<EmployeeListDto>
            {
                new() { Id = 1, FullName = "John Doe", Position = "Dev", DepartmentName = "Eng", IsActive = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(1, 10, null)).ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(1, 10, null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<EmployeeListDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.TotalCount.Should().Be(1);
    }
}
