using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Responses;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Employee Management API — v1.
    /// Provides full CRUD operations for employees.
    /// All endpoints require JWT Bearer authentication.
    /// Role-based access control is applied per endpoint.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize]   // base requirement: valid JWT token
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
        // GET /api/v1/employees
        // Roles: Admin, Manager, Employee — any authenticated user
        // ===================================================================
        /// <summary>Get all employees.</summary>
        /// <remarks>
        /// Returns a list of all employees including their department details.
        /// Accessible by: **Admin**, **Manager**, **Employee**
        /// </remarks>
        /// <returns>200 OK — list of EmployeeReadDto wrapped in ApiResponse.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Employee")]
        [ProducesResponseType(typeof(ApiResponse<List<EmployeeReadDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmployees()
        {
            _logger.LogInformation("GET /api/v1/employees — user: {User}, role: {Role}",
                User.Identity?.Name, User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);

            var employees = await _employeeService.GetAllEmployeesAsync();

            _logger.LogInformation("GET /api/v1/employees — returned {Count} records", employees.Count);

            return Ok(ApiResponse<List<EmployeeReadDto>>.SuccessResponse(
                employees, $"Successfully retrieved {employees.Count} employee(s)."));
        }

        // ===================================================================
        // GET /api/v1/employees/{id}
        // Roles: Admin, Manager, Employee
        // ===================================================================
        /// <summary>Get a specific employee by ID.</summary>
        /// <param name="id">The employee primary key (integer).</param>
        /// <remarks>
        /// Returns a single employee with department details.
        /// Returns **404** if the employee does not exist.
        /// Accessible by: **Admin**, **Manager**, **Employee**
        /// </remarks>
        /// <returns>200 OK with EmployeeReadDto, or 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            _logger.LogInformation("GET /api/v1/employees/{Id} — user: {User}", id, User.Identity?.Name);

            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("GET /api/v1/employees/{Id} — not found", id);
                return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));
            }

            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(employee));
        }

        // ===================================================================
        // POST /api/v1/employees
        // Roles: Admin, Manager only
        // ===================================================================
        /// <summary>Create a new employee.</summary>
        /// <param name="createDto">Employee data. EmployeeId is auto-generated — do not include it.</param>
        /// <remarks>
        /// Creates a new employee record.
        /// All fields except **Phone** are required.
        /// **DepartmentId** must be 1 (Engineering), 2 (HR), 3 (Finance), or 4 (Marketing).
        /// Returns **400** if validation fails.
        /// Accessible by: **Admin**, **Manager** only.
        /// </remarks>
        /// <returns>201 Created with the new employee and a Location header.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeCreateDto createDto)
        {
            _logger.LogInformation("POST /api/v1/employees — user: {User}", User.Identity?.Name);

            var created = await _employeeService.CreateEmployeeAsync(createDto);

            _logger.LogInformation("POST /api/v1/employees — created ID {Id}", created.EmployeeId);

            return CreatedAtAction(nameof(GetEmployeeById),
                new { id = created.EmployeeId },
                ApiResponse<EmployeeReadDto>.CreatedResponse(created));
        }

        // ===================================================================
        // PUT /api/v1/employees/{id}
        // Roles: Admin, Manager only
        // ===================================================================
        /// <summary>Update an existing employee (full replacement).</summary>
        /// <param name="id">The employee primary key from the URL.</param>
        /// <param name="updateDto">Complete updated employee data. EmployeeId in body must match route ID.</param>
        /// <remarks>
        /// Replaces all updatable fields of the employee.
        /// Returns **400** if the route ID does not match the body EmployeeId.
        /// Returns **404** if the employee does not exist.
        /// Accessible by: **Admin**, **Manager** only.
        /// </remarks>
        /// <returns>200 OK with the updated employee.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] EmployeeUpdateDto updateDto)
        {
            if (id != updateDto.EmployeeId)
            {
                _logger.LogWarning("PUT ID mismatch — route: {RouteId}, body: {BodyId}", id, updateDto.EmployeeId);
                return BadRequest(ApiResponse<EmployeeReadDto>.BadRequestResponse(
                    $"Route ID ({id}) does not match body EmployeeId ({updateDto.EmployeeId})."));
            }

            _logger.LogInformation("PUT /api/v1/employees/{Id} — user: {User}", id, User.Identity?.Name);

            var updated = await _employeeService.UpdateEmployeeAsync(id, updateDto);
            if (updated == null)
                return NotFound(ApiResponse<EmployeeReadDto>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));

            return Ok(ApiResponse<EmployeeReadDto>.SuccessResponse(updated, "Employee updated successfully."));
        }

        // ===================================================================
        // DELETE /api/v1/employees/{id}
        // Roles: Admin only
        // ===================================================================
        /// <summary>Delete an employee by ID.</summary>
        /// <param name="id">The employee primary key.</param>
        /// <remarks>
        /// Permanently removes the employee from the database.
        /// Returns **404** if the employee does not exist.
        /// Returns **403** if the caller does not have the **Admin** role.
        /// Accessible by: **Admin** only.
        /// </remarks>
        /// <returns>200 OK on success, 404 if not found, 403 if unauthorized role.</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.LogInformation("DELETE /api/v1/employees/{Id} — user: {User}", id, User.Identity?.Name);

            var deleted = await _employeeService.DeleteEmployeeAsync(id);
            if (!deleted)
                return NotFound(ApiResponse<object>.NotFoundResponse(
                    $"Employee with ID {id} was not found."));

            _logger.LogInformation("DELETE /api/v1/employees/{Id} — deleted successfully", id);
            return Ok(ApiResponse<object>.SuccessResponse(null!,
                $"Employee with ID {id} was deleted successfully."));
        }
    }
}
