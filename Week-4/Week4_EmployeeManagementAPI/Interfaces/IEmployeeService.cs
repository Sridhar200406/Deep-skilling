using Week3_EmployeeManagementAPI.DTOs;

namespace Week3_EmployeeManagementAPI.Interfaces
{
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
