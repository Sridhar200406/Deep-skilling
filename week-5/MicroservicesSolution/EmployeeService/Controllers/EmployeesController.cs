using Microsoft.AspNetCore.Mvc;
using EmployeeService.DTOs;
using EmployeeService.Interfaces;
using EmployeeService.Responses;

namespace EmployeeService.Controllers
{
    /// <summary>
    /// Employee Microservice API.
    /// Provides full CRUD operations for employees.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger          = logger;
        }

        // ===================================================================
        // GET /api/employees
        // ===================================================================
        /// <summary>Get all employees with pagination, filtering, and sorting.</summary>
        /// <param name="queryParams">Optional query parameters for filtering, sorting, and pagination.</param>
        /// <returns>200 OK — list of EmployeeReadDto wrapped in ApiResponse.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeReadDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmployees([FromQuery] EmployeeQueryParameters queryParams)
        {
            _logger.LogInformation("GET /api/employees — params: {@Params}", queryParams);

            var employees = await _employeeService.GetAllEmployeesAsync(queryParams);

            _logger.LogInformation("GET /api/employees — returned {Count} records", employees.Count);

            return Ok(ApiResponse<List<EmployeeReadDto>>.SuccessResponse(
                employees, $"Successfully retrieved {employees.Count} employee(s)."));
        }

        // ===================================================================
        // GET /api/employees/{id}
        // ===================================================================
        /// <summary>Get a specific employee by ID.</summary>
        /// <param name="id">The employee primary key (integer).</param>
        /// <returns>200 OK with EmployeeReadDto, or 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            _logger.LogInformation("GET /api/employees/{Id}", id);

            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("GET /api/employees/{Id} — not found", id);
                return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));
            }

            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(employee));
        }

        // ===================================================================
        // POST /api/employees
        // ===================================================================
        /// <summary>Create a new employee.</summary>
        /// <param name="createDto">Employee data. EmployeeId is auto-generated.</param>
        /// <returns>201 Created with the new employee and a Location header.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeCreateDto createDto)
        {
            _logger.LogInformation("POST /api/employees");

            var created = await _employeeService.CreateEmployeeAsync(createDto);

            _logger.LogInformation("POST /api/employees — created ID {Id}", created.EmployeeId);

            return CreatedAtAction(nameof(GetEmployeeById),
                new { id = created.EmployeeId },
                ApiResponse<EmployeeReadDto>.CreatedResponse(created));
        }

        // ===================================================================
        // PUT /api/employees/{id}
        // ===================================================================
        /// <summary>Update an existing employee (full replacement).</summary>
        /// <param name="id">The employee primary key from the URL.</param>
        /// <param name="updateDto">Complete updated employee data.</param>
        /// <returns>200 OK with the updated employee.</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] EmployeeUpdateDto updateDto)
        {
            if (id != updateDto.EmployeeId)
            {
                _logger.LogWarning("PUT ID mismatch — route: {RouteId}, body: {BodyId}", id, updateDto.EmployeeId);
                return BadRequest(ApiResponse<EmployeeReadDto>.BadRequestResponse(
                    $"Route ID ({id}) does not match body EmployeeId ({updateDto.EmployeeId})."));
            }

            _logger.LogInformation("PUT /api/employees/{Id}", id);

            var updated = await _employeeService.UpdateEmployeeAsync(id, updateDto);
            if (updated == null)
                return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));

            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(updated, "Employee updated successfully."));
        }

        // ===================================================================
        // DELETE /api/employees/{id}
        // ===================================================================
        /// <summary>Delete an employee by ID.</summary>
        /// <param name="id">The employee primary key.</param>
        /// <returns>200 OK on success, 404 if not found.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.LogInformation("DELETE /api/employees/{Id}", id);

            var deleted = await _employeeService.DeleteEmployeeAsync(id);
            if (!deleted)
                return NotFound(ApiResponse<object>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));

            _logger.LogInformation("DELETE /api/employees/{Id} — deleted successfully", id);
            return Ok(ApiResponse<object>.SuccessResponse(null!,
                $"Employee with ID {id} was deleted successfully."));
        }
    }
}
