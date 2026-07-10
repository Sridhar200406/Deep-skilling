using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Contract for employee business logic operations.
    /// Keeps the controller thin — all data access goes through this interface.
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>Returns all employees including their department.</summary>
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();

        /// <summary>Returns a single employee by ID, or null if not found.</summary>
        Task<Employee?> GetEmployeeByIdAsync(int id);

        /// <summary>Creates a new employee. Returns the created employee with its generated ID.</summary>
        Task<Employee> CreateEmployeeAsync(Employee employee);

        /// <summary>
        /// Updates an existing employee. Returns the updated employee,
        /// or null if the ID does not exist.
        /// </summary>
        Task<Employee?> UpdateEmployeeAsync(int id, Employee employee);

        /// <summary>
        /// Deletes an employee by ID.
        /// Returns true if deleted, false if not found.
        /// </summary>
        Task<bool> DeleteEmployeeAsync(int id);

        /// <summary>Checks whether an employee with the given ID exists.</summary>
        Task<bool> EmployeeExistsAsync(int id);
    }
}
