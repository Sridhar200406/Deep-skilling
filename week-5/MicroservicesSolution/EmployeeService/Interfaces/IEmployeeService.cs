using EmployeeService.DTOs;

namespace EmployeeService.Interfaces
{
    /// <summary>Service contract for Employee business logic.</summary>
    public interface IEmployeeService
    {
        Task<List<EmployeeReadDto>> GetAllEmployeesAsync(EmployeeQueryParameters queryParams);
        Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id);
        Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto);
        Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> EmployeeExistsAsync(int id);
    }
}
