using DepartmentService.Models;

namespace DepartmentService.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<List<Department>> GetAllAsync();
        Task<Department?> GetByIdAsync(int id);
        Task AddAsync(Department d);
        Task UpdateAsync(Department d);
        Task DeleteAsync(Department d);
        Task<bool> ExistsAsync(int id);
    }
}
