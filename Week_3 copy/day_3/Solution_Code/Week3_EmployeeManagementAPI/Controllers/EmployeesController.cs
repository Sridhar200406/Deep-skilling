using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// Full CRUD REST API controller for the Employee resource.
    /// Day 3: depends on IEmployeeService (from Interfaces/) — no repository or DbContext awareness.
    /// Base route: /api/employees
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        // Both dependencies are resolved by ASP.NET Core DI
        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        // ===================================================================
        // GET /api/employees
        // ===================================================================
        /// <summary>Get all employees.</summary>
        /// <returns>200 OK — JSON array of all employees with department details.</returns>
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
        // ===================================================================
        /// <summary>Create a new employee.</summary>
        /// <param name="employee">Employee data — EmployeeId is ignored (auto-generated).</param>
        /// <returns>201 Created with the new employee and Location header.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEmployee([FromBody] Employee employee)
        {
            try
            {
                _logger.LogInformation("POST /api/employees — creating new employee.");
                var created = await _employeeService.CreateEmployeeAsync(employee);

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
        // ===================================================================
        /// <summary>Update an existing employee.</summary>
        /// <param name="id">The employee primary key from the URL.</param>
        /// <param name="employee">Full updated employee data.</param>
        /// <returns>200 OK with updated employee, or 400/404.</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee employee)
        {
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
        // ===================================================================
        /// <summary>Delete an employee by ID.</summary>
        /// <param name="id">The employee primary key.</param>
        /// <returns>204 No Content on success, or 404 Not Found.</returns>
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
