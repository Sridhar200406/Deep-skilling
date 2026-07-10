using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.Models;
using Week3_EmployeeManagementAPI.Services;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Full CRUD REST API controller for the Employee resource.
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
        // Returns all employees with their department details.
        // ===================================================================
        /// <summary>Get all employees.</summary>
        /// <returns>200 OK — JSON array of all employees.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                _logger.LogInformation("GET /api/employees — fetching all employees.");
                var employees = await _employeeService.GetAllEmployeesAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all employees.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving employees." });
            }
        }

        // ===================================================================
        // GET /api/employees/{id}
        // Returns a single employee by primary key.
        // ===================================================================
        /// <summary>Get a specific employee by ID.</summary>
        /// <param name="id">The employee primary key.</param>
        /// <returns>200 OK with the employee, or 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            try
            {
                _logger.LogInformation("GET /api/employees/{Id}", id);
                var employee = await _employeeService.GetEmployeeByIdAsync(id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee {Id} not found.", id);
                    return NotFound(new { message = $"Employee with ID {id} was not found." });
                }

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the employee." });
            }
        }

        // ===================================================================
        // POST /api/employees
        // Creates a new employee.
        // Request body: Employee JSON (without EmployeeId — DB generates it).
        // Response: 201 Created with the new employee including generated ID.
        // ===================================================================
        /// <summary>Create a new employee.</summary>
        /// <param name="employee">Employee data (EmployeeId is ignored — auto-generated).</param>
        /// <returns>201 Created with the created employee and Location header.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmployee([FromBody] Employee employee)
        {
            // [ApiController] automatically returns 400 if ModelState is invalid
            // (e.g. missing [Required] fields), so no manual ModelState check needed.
            try
            {
                _logger.LogInformation("POST /api/employees — creating new employee.");
                var created = await _employeeService.CreateEmployeeAsync(employee);

                // 201 Created + Location: /api/employees/{id}
                return CreatedAtAction(
                    nameof(GetEmployeeById),
                    new { id = created.EmployeeId },
                    created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the employee." });
            }
        }

        // ===================================================================
        // PUT /api/employees/{id}
        // Replaces all updatable fields of an existing employee.
        // Request body: Full Employee JSON including all fields.
        // Response: 200 OK with the updated employee.
        // ===================================================================
        /// <summary>Update an existing employee.</summary>
        /// <param name="id">The employee primary key from the URL.</param>
        /// <param name="employee">Updated employee data.</param>
        /// <returns>200 OK with updated employee, or 400/404.</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee employee)
        {
            // Route ID and body ID must match to prevent accidental updates
            if (id != employee.EmployeeId)
            {
                return BadRequest(new
                {
                    message = $"Route ID ({id}) does not match body EmployeeId ({employee.EmployeeId})."
                });
            }

            try
            {
                _logger.LogInformation("PUT /api/employees/{Id}", id);
                var updated = await _employeeService.UpdateEmployeeAsync(id, employee);

                if (updated == null)
                {
                    _logger.LogWarning("Employee {Id} not found for update.", id);
                    return NotFound(new { message = $"Employee with ID {id} was not found." });
                }

                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the employee." });
            }
        }

        // ===================================================================
        // DELETE /api/employees/{id}
        // Deletes an employee by ID.
        // Response: 204 No Content on success, 404 if not found.
        // ===================================================================
        /// <summary>Delete an employee by ID.</summary>
        /// <param name="id">The employee primary key.</param>
        /// <returns>204 No Content, or 404 Not Found.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                _logger.LogInformation("DELETE /api/employees/{Id}", id);
                var deleted = await _employeeService.DeleteEmployeeAsync(id);

                if (!deleted)
                {
                    _logger.LogWarning("Employee {Id} not found for deletion.", id);
                    return NotFound(new { message = $"Employee with ID {id} was not found." });
                }

                // 204 — success, no body
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while deleting the employee." });
            }
        }
    }
}
