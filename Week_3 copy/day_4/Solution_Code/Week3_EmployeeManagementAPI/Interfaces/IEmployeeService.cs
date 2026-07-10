using Week3_EmployeeManagementAPI.DTOs;

namespace Week3_EmployeeManagementAPI.Interfaces
{
    /// <summary>
    /// Service contract — Day 4 update.
    /// All methods now use DTOs instead of Employee entities.
    /// The controller never sees the Employee model — only DTOs cross the service boundary.
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>Returns all employees as read DTOs.</summary>
        Task<List<EmployeeReadDto>> GetAllEmployeesAsync();

        /// <summary>Returns one employee as a read DTO, or null if not found.</summary>
        Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id);

        /// <summary>Creates a new employee from the create DTO. Returns the created employee as a read DTO.</summary>
        Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto);

        /// <summary>
        /// Updates an existing employee from the update DTO.
        /// Returns the updated employee as a read DTO, or null if the ID does not exist.
        /// </summary>
        Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto);

        /// <summary>Deletes an employee by ID. Returns true if deleted, false if not found.</summary>
        Task<bool> DeleteEmployeeAsync(int id);

        /// <summary>Returns true if an employee with the given ID exists.</summary>
        Task<bool> EmployeeExistsAsync(int id);
    }
}
