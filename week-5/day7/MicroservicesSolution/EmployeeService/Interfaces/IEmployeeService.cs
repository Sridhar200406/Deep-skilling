using EmployeeService.DTOs;

namespace EmployeeService.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<EmployeeReadDto>> GetAllEmployeesAsync(EmployeeQueryParameters q);
        Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id);
        Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto dto);
        Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto dto);
        Task<bool> DeleteEmployeeAsync(int id);
    }
}
