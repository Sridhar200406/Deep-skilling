using EmployeeService.DTOs;
using EmployeeService.Models;

namespace EmployeeService.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync(EmployeeQueryParameters q);
        Task<Employee?> GetByIdAsync(int id);
        Task AddAsync(Employee e);
        Task UpdateAsync(Employee e);
        Task DeleteAsync(Employee e);
        Task<bool> ExistsAsync(int id);
    }
}
