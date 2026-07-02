using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Interfaces
{
    /// <summary>
    /// Repository contract — defines all data-access operations for the Employee entity.
    /// The repository speaks directly to the database (via AppDbContext).
    /// Nothing outside the repository folder should know about EF Core.
    /// </summary>
    public interface IEmployeeRepository
    {
        /// <summary>Returns all employees with their related Department (eager-loaded).</summary>
        Task<List<Employee>> GetAllAsync();

        /// <summary>Returns one employee by primary key, including Department, or null.</summary>
        Task<Employee?> GetByIdAsync(int id);

        /// <summary>
        /// Persists a new employee to the database.
        /// The generated EmployeeId is set on the entity after this call.
        /// </summary>
        Task AddAsync(Employee employee);

        /// <summary>Persists changes made to a tracked employee entity.</summary>
        Task UpdateAsync(Employee employee);

        /// <summary>Removes the employee entity from the database.</summary>
        Task DeleteAsync(Employee employee);

        /// <summary>Returns true if an employee with the given ID exists.</summary>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Loads the Department navigation property on an already-tracked employee.
        /// Used after Add/Update to populate the response body.
        /// </summary>
        Task LoadDepartmentAsync(Employee employee);
    }
}
