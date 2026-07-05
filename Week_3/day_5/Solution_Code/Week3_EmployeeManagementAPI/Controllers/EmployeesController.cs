using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Responses;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Full CRUD REST API controller — Day 5 update.
    /// All responses are wrapped in ApiResponse&lt;T&gt; for a consistent response shape.
    /// Global exceptions are handled by ExceptionMiddleware — no try/catch needed here.
    /// Base route: /api/employees
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
            _logger = logger;
        }

        // ===================================================================
        // GET /api/employees
        // ===================================================================
        /// <summary>Get all employees.</summary>
        /// <remarks>Returns a standardized ApiResponse containing a list of all employees with department details.</remarks>
        /// <returns>200 OK — ApiResponse wrapping List of EmployeeReadDto.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeReadDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmployees()
        {
            _logger.LogInformation("GET /api/employees — fetching all employees at {Time}", DateTime.UtcNow);

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
        /// <remarks>Returns 404 if the employee does not exist.</remarks>
        /// <returns>200 OK with EmployeeReadDto wrapped in ApiResponse, or 404.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            _logger.LogInformation("GET /api/employees/{Id} — fetching employee", id);

            var employee = await _employeeService.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                _logger.LogWarning("GET /api/employees/{Id} — employee not found", id);
                return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));
            }

            _logger.LogInformation("GET /api/employees/{Id} — found employee: {FullName}", id, employee.FullName);
            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(employee));
        }

        // ===================================================================
        // POST /api/employees
        // ===================================================================
        /// <summary>Create a new employee.</summary>
        /// <param name="createDto">Employee create data — EmployeeId is not required (auto-generated).</param>
        /// <remarks>
        /// All fields except Phone are required.
        /// DepartmentId must reference an existing department (1–4).
        /// Returns 400 if validation fails.
        /// </remarks>
        /// <returns>201 Created with the new employee wrapped in ApiResponse.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeCreateDto createDto)
        {
            // [ApiController] handles model validation automatically — returns 400 if invalid
            _logger.LogInformation("POST /api/employees — creating employee: {FirstName} {LastName}",
                createDto.FirstName, createDto.LastName);

            var created = await _employeeService.CreateEmployeeAsync(createDto);

            _logger.LogInformation("POST /api/employees — created employee with ID {Id}", created.EmployeeId);

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
        /// <param name="updateDto">Full updated employee data. EmployeeId in body must match route ID.</param>
        /// <remarks>
        /// Route ID must match EmployeeId in the request body.
        /// Returns 400 on mismatch, 404 if employee not found.
        /// </remarks>
        /// <returns>200 OK with updated EmployeeReadDto wrapped in ApiResponse.</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] EmployeeUpdateDto updateDto)
        {
            if (id != updateDto.EmployeeId)
            {
                _logger.LogWarning(
                    "PUT /api/employees/{RouteId} — ID mismatch: body contains EmployeeId={BodyId}",
                    id, updateDto.EmployeeId);

                return BadRequest(ApiResponse<EmployeeReadDto>.BadRequestResponse(
                    $"Route ID ({id}) does not match body EmployeeId ({updateDto.EmployeeId})."));
            }

            _logger.LogInformation("PUT /api/employees/{Id} — updating employee", id);

            var updated = await _employeeService.UpdateEmployeeAsync(id, updateDto);

            if (updated == null)
            {
                _logger.LogWarning("PUT /api/employees/{Id} — employee not found for update", id);
                return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));
            }

            _logger.LogInformation("PUT /api/employees/{Id} — employee updated successfully", id);
            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(updated, "Employee updated successfully."));
        }

        // ===================================================================
        // DELETE /api/employees/{id}
        // ===================================================================
        /// <summary>Delete an employee by ID.</summary>
        /// <param name="id">The employee primary key.</param>
        /// <remarks>Returns 404 if the employee does not exist.</remarks>
        /// <returns>200 OK with a success ApiResponse, or 404.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.LogInformation("DELETE /api/employees/{Id} — deleting employee", id);

            var deleted = await _employeeService.DeleteEmployeeAsync(id);

            if (!deleted)
            {
                _logger.LogWarning("DELETE /api/employees/{Id} — employee not found for deletion", id);
                return NotFound(ApiResponse<object>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));
            }

            _logger.LogInformation("DELETE /api/employees/{Id} — employee deleted successfully", id);

            // Return 200 with message instead of 204 so the ApiResponse body is visible
            return Ok(ApiResponse<object>.SuccessResponse(null!, $"Employee with ID {id} was deleted successfully."));
        }
    }
}
