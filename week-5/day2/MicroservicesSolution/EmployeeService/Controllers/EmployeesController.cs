using EmployeeService.DTOs;
using EmployeeService.Interfaces;
using EmployeeService.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeService.Controllers
{
    /// <summary>
    /// Employee CRUD API.
    /// Day 2: DepartmentName is now enriched via inter-service HTTP call.
    /// All routes require JWT (validated at Gateway + here for direct access).
    /// </summary>
    [ApiController]
    [Route("api/employees")]
    [Authorize]
    [Produces("application/json")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _svc;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeService svc, ILogger<EmployeesController> logger)
        {
            _svc    = svc;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] EmployeeQueryParameters q)
        {
            var list = await _svc.GetAllEmployeesAsync(q);
            return Ok(ApiResponse<List<EmployeeReadDto>>.SuccessResponse(list, $"Retrieved {list.Count} employee(s)."));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var emp = await _svc.GetEmployeeByIdAsync(id);
            if (emp == null) return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse($"Employee {id} not found."));
            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(emp));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeCreateDto dto)
        {
            var created = await _svc.CreateEmployeeAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.EmployeeId },
                ApiResponse<EmployeeReadDto>.CreatedResponse(created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDto dto)
        {
            if (id != dto.EmployeeId)
                return BadRequest(ApiResponse<EmployeeReadDto>.BadRequestResponse("Route id and body EmployeeId mismatch."));
            var updated = await _svc.UpdateEmployeeAsync(id, dto);
            if (updated == null) return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse($"Employee {id} not found."));
            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(updated, "Updated successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _svc.DeleteEmployeeAsync(id);
            if (!deleted) return NotFound(ApiResponse<object>.NotFoundResponse($"Employee {id} not found."));
            return Ok(ApiResponse<object>.SuccessResponse(null!, $"Employee {id} deleted."));
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() =>
            Ok(new { Status = "Healthy", Service = "EmployeeService", Timestamp = DateTime.UtcNow });
    }
}
