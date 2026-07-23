using DepartmentService.Models;

namespace DepartmentService.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<List<Department>> GetAllAsync();
        Task<Department?> GetByIdAsync(int id);
        Task AddAsync(Department department);
        Task UpdateAsync(Department department);
        Task DeleteAsync(Department department);
        Task<bool> ExistsAsync(int id);
    }

    public interface IDepartmentService
    {
        Task<List<DTOs.DepartmentReadDto>> GetAllAsync();
        Task<DTOs.DepartmentReadDto?> GetByIdAsync(int id);
        Task<DTOs.DepartmentReadDto> CreateAsync(DTOs.DepartmentCreateDto dto);
        Task<DTOs.DepartmentReadDto?> UpdateAsync(int id, DTOs.DepartmentUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
