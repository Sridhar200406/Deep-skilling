using EmployeeService.Domain.Entities;
using EmployeeService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace EmployeeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly EmployeeDbContext _context;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(EmployeeDbContext context, ILogger<DepartmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Get all active departments</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _context.Departments
            .Where(d => d.IsActive)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Description,
                EmployeeCount = d.Employees.Count(e => e.IsActive)
            })
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(departments));
    }

    /// <summary>Get a department by ID</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dept = await _context.Departments
            .Include(d => d.Employees.Where(e => e.IsActive))
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

        if (dept == null)
            return NotFound(ApiResponse<object>.FailureResponse($"Department {id} not found."));

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            dept.Id,
            dept.Name,
            dept.Description,
            Employees = dept.Employees.Select(e => new { e.Id, FullName = $"{e.FirstName} {e.LastName}", e.Position })
        }));
    }

    /// <summary>Create a new department</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation failed"));

        if (await _context.Departments.AnyAsync(d => d.Name == request.Name))
            return BadRequest(ApiResponse<object>.FailureResponse($"Department '{request.Name}' already exists."));

        var dept = new Department
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dept.Id },
            ApiResponse<object>.SuccessResponse(dept, "Department created."));
    }
}

public record CreateDepartmentRequest(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2)]
    string Name,
    string Description = "");
