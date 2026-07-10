using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.Services;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// REST API controller for Employee resource.
    /// Base route: /api/employees
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        // IEmployeeService and ILogger are both resolved by DI
        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        // -----------------------------------------------------------------------
        // GET /api/employees
        // Returns a list of all employees with their department details.
        // -----------------------------------------------------------------------
        /// <summary>Get all employees.</summary>
        /// <returns>200 OK with a JSON array of employees.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                _logger.LogInformation("Fetching all employees.");
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

        // -----------------------------------------------------------------------
        // GET /api/employees/{id}
        // Returns a single employee by their primary key.
        // -----------------------------------------------------------------------
        /// <summary>Get a specific employee by ID.</summary>
        /// <param name="id">The employee's primary key.</param>
        /// <returns>200 OK with the employee, or 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            try
            {
                _logger.LogInformation("Fetching employee with ID {EmployeeId}.", id);
                var employee = await _employeeService.GetEmployeeByIdAsync(id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found.", id);
                    return NotFound(new { message = $"Employee with ID {id} was not found." });
                }

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee with ID {EmployeeId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the employee." });
            }
        }
    }
}
