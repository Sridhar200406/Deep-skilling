using DepartmentService.Data;
using DepartmentService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace DepartmentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentDbContext _db;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(DepartmentDbContext db, ILogger<DepartmentsController> logger)
    {
        _db = db; _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var depts = await _db.Departments.Where(d => d.IsActive)
            .OrderBy(d => d.Name).ToListAsync();
        return Ok(ApiResponse<List<Department>>.SuccessResponse(depts));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null || !dept.IsActive)
            return NotFound(ApiResponse<object>.FailureResponse($"Department {id} not found."));
        return Ok(ApiResponse<Department>.SuccessResponse(dept));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation failed."));

        if (await _db.Departments.AnyAsync(d => d.Name == dto.Name))
            return BadRequest(ApiResponse<object>.FailureResponse($"Department '{dto.Name}' exists."));

        var dept = new Department { Name = dto.Name.Trim(), Description = dto.Description?.Trim() ?? "" };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Department '{Name}' created (Id={Id})", dept.Name, dept.Id);
        return CreatedAtAction(nameof(GetById), new { id = dept.Id },
            ApiResponse<Department>.SuccessResponse(dept));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateDepartmentDto dto)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return NotFound(ApiResponse<object>.FailureResponse($"Department {id} not found."));

        dept.Name = dto.Name.Trim();
        dept.Description = dto.Description?.Trim() ?? dept.Description;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<Department>.SuccessResponse(dept));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept == null) return NotFound(ApiResponse<object>.FailureResponse($"Department {id} not found."));
        dept.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { id }, "Department deactivated."));
    }
}

public record CreateDepartmentDto(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2)]
    string Name,
    string? Description = null);
