using Week3_EmployeeManagementAPI.DTOs;

namespace Week3_EmployeeManagementAPI.Interfaces
{
    /// <summary>
    /// Service contract — unchanged from Day 4.
    /// All methods use DTOs. Employee entity never crosses the service boundary outward.
    /// </summary>
    public interface IEmployeeService
    {
        Task<List<EmployeeReadDto>> GetAllEmployeesAsync();
        Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id);
        Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto);
        Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> EmployeeExistsAsync(int id);
    }
}
