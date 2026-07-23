using EmployeeService.DTOs;
using EmployeeService.Models;

namespace EmployeeService.Interfaces
{
    /// <summary>Repository contract for Employee data access.</summary>
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync(EmployeeQueryParameters queryParams);
        Task<Employee?> GetByIdAsync(int id);
        Task AddAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        Task DeleteAsync(Employee employee);
        Task<bool> ExistsAsync(int id);
    }
}
