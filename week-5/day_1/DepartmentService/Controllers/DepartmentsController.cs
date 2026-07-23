using Microsoft.AspNetCore.Mvc;
using DepartmentService.DTOs;
using DepartmentService.Interfaces;

namespace DepartmentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _service;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentService service, ILogger<DepartmentsController> logger)
        { _service = service; _logger = logger; }

        /// <summary>Get all departments.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _service.GetAllAsync();
            return Ok(new { Success = true, Data = departments, Message = $"Retrieved {departments.Count} department(s)." });
        }

        /// <summary>Get department by ID.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var department = await _service.GetByIdAsync(id);
            if (department == null) return NotFound(new { Success = false, Message = $"Department {id} not found." });
            return Ok(new { Success = true, Data = department });
        }

        /// <summary>Create a new department.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.DepartmentId },
                new { Success = true, Data = created, Message = "Department created." });
        }

        /// <summary>Update an existing department.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentUpdateDto dto)
        {
            if (id != dto.DepartmentId)
                return BadRequest(new { Success = false, Message = "ID mismatch." });
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound(new { Success = false, Message = $"Department {id} not found." });
            return Ok(new { Success = true, Data = updated, Message = "Department updated." });
        }

        /// <summary>Delete a department.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound(new { Success = false, Message = $"Department {id} not found." });
            return Ok(new { Success = true, Message = $"Department {id} deleted." });
        }
    }
}
