using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Interfaces
{
    /// <summary>
    /// Service contract — defines all business-logic operations for Employee.
    /// The controller depends only on this interface, never on the repository or DbContext.
    /// Moving IEmployeeService here (from Services/) keeps all contracts in one place.
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>Returns all employees including their department.</summary>
        Task<List<Employee>> GetAllEmployeesAsync();

        /// <summary>Returns a single employee by ID, or null if not found.</summary>
        Task<Employee?> GetEmployeeByIdAsync(int id);

        /// <summary>Creates a new employee and returns it with the generated ID.</summary>
        Task<Employee> CreateEmployeeAsync(Employee employee);

        /// <summary>
        /// Updates an existing employee.
        /// Returns the updated employee or null if the ID does not exist.
        /// </summary>
        Task<Employee?> UpdateEmployeeAsync(int id, Employee employee);

        /// <summary>
        /// Deletes an employee by ID.
        /// Returns true if deleted, false if not found.
        /// </summary>
        Task<bool> DeleteEmployeeAsync(int id);

        /// <summary>Returns true if an employee with the given ID exists.</summary>
        Task<bool> EmployeeExistsAsync(int id);
    }
}
