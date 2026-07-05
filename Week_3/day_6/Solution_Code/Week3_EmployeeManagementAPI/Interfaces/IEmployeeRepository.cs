using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();
        Task<Employee?> GetByIdAsync(int id);
        Task AddAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        Task DeleteAsync(Employee employee);
        Task<bool> ExistsAsync(int id);
        Task LoadDepartmentAsync(Employee employee);
    }
}
