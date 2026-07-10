using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Responses;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Full CRUD REST API controller — Day 6 update.
    /// All endpoints are protected with [Authorize] — a valid JWT Bearer token is required.
    /// All responses are wrapped in ApiResponse&lt;T&gt;.
    /// Base route: /api/employees
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]   // ← Day 6: all endpoints require a valid JWT token
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        // ===================================================================
        // GET /api/employees
        // ===================================================================
        /// <summary>Get all employees.</summary>
        /// <remarks>Requires Bearer token. Returns all employees with department details.</remarks>
        /// <returns>200 OK — ApiResponse wrapping List of EmployeeReadDto.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeReadDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmployees()
        {
            _logger.LogInformation("GET /api/employees — user: {User}", User.Identity?.Name);

            var employees = await _employeeService.GetAllEmployeesAsync();

            _logger.LogInformation("GET /api/employees — returned {Count} employees", employees.Count);

            return Ok(ApiResponse<List<EmployeeReadDto>>.SuccessResponse(
                employees,
                $"Successfully retrieved {employees.Count} employee(s)."));
        }

        // ===================================================================
        // GET /api/employees/{id}
        // ===================================================================
        /// <summary>Get a specific employee by ID.</summary>
        /// <param name="id">The employee primary key.</param>
        /// <remarks>Requires Bearer token. Returns 404 if employee does not exist.</remarks>
        /// <returns>200 OK with EmployeeReadDto, or 404.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            _logger.LogInformation("GET /api/employees/{Id} — user: {User}", id, User.Identity?.Name);

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
        /// <param name="createDto">Employee create data — EmployeeId not required.</param>
        /// <remarks>Requires Bearer token. Returns 400 if validation fails.</remarks>
        /// <returns>201 Created with EmployeeReadDto and Location header.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeCreateDto createDto)
        {
            _logger.LogInformation("POST /api/employees — user: {User}", User.Identity?.Name);

            var created = await _employeeService.CreateEmployeeAsync(createDto);

            _logger.LogInformation("POST /api/employees — created ID {Id}", created.EmployeeId);

            return CreatedAtAction(
                nameof(GetEmployeeById),
                new { id = created.EmployeeId },
                ApiResponse<EmployeeReadDto>.CreatedResponse(created));
        }

        // ===================================================================
        // PUT /api/employees/{id}
        // ===================================================================
        /// <summary>Update an existing employee.</summary>
        /// <param name="id">The employee primary key from the URL.</param>
        /// <param name="updateDto">Full updated employee data. EmployeeId must match route ID.</param>
        /// <remarks>Requires Bearer token. Returns 400 on ID mismatch, 404 if not found.</remarks>
        /// <returns>200 OK with updated EmployeeReadDto.</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] EmployeeUpdateDto updateDto)
        {
            if (id != updateDto.EmployeeId)
            {
                _logger.LogWarning("PUT mismatch: route={RouteId} body={BodyId}", id, updateDto.EmployeeId);
                return BadRequest(ApiResponse<EmployeeReadDto>.BadRequestResponse(
                    $"Route ID ({id}) does not match body EmployeeId ({updateDto.EmployeeId})."));
            }

            _logger.LogInformation("PUT /api/employees/{Id} — user: {User}", id, User.Identity?.Name);

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
        /// <remarks>Requires Bearer token. Returns 404 if not found.</remarks>
        /// <returns>200 OK on success, or 404.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.LogInformation("DELETE /api/employees/{Id} — user: {User}", id, User.Identity?.Name);

            var deleted = await _employeeService.DeleteEmployeeAsync(id);

            if (!deleted)
                return NotFound(ApiResponse<object>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));

            return Ok(ApiResponse<object>.SuccessResponse(null!,
                $"Employee with ID {id} was deleted successfully."));
        }
    }
}
