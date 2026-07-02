using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Contract for employee business logic operations.
    /// Keeps the controller thin — all data access goes through this interface.
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>Returns all active employees including their department.</summary>
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();

        /// <summary>Returns a single employee by ID, or null if not found.</summary>
        Task<Employee?> GetEmployeeByIdAsync(int id);
    }
}
