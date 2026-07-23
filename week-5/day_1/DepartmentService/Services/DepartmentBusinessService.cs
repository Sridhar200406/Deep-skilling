using DepartmentService.DTOs;
using DepartmentService.Interfaces;
using DepartmentService.Models;

namespace DepartmentService.Services
{
    public class DepartmentBusinessService : IDepartmentService
    {
        private readonly IDepartmentRepository _repo;
        private readonly ILogger<DepartmentBusinessService> _logger;

        public DepartmentBusinessService(IDepartmentRepository repo, ILogger<DepartmentBusinessService> logger)
        { _repo = repo; _logger = logger; }

        public async Task<List<DepartmentReadDto>> GetAllAsync()
        {
            var departments = await _repo.GetAllAsync();
            return departments.Select(d => new DepartmentReadDto
            {
                DepartmentId = d.DepartmentId, DepartmentName = d.DepartmentName, Description = d.Description
            }).ToList();
        }

        public async Task<DepartmentReadDto?> GetByIdAsync(int id)
        {
            var d = await _repo.GetByIdAsync(id);
            return d == null ? null : new DepartmentReadDto
            {
                DepartmentId = d.DepartmentId, DepartmentName = d.DepartmentName, Description = d.Description
            };
        }

        public async Task<DepartmentReadDto> CreateAsync(DepartmentCreateDto dto)
        {
            var department = new Department { DepartmentName = dto.DepartmentName, Description = dto.Description };
            await _repo.AddAsync(department);
            _logger.LogInformation("Created department ID {Id}", department.DepartmentId);
            return new DepartmentReadDto
            {
                DepartmentId = department.DepartmentId, DepartmentName = department.DepartmentName, Description = department.Description
            };
        }

        public async Task<DepartmentReadDto?> UpdateAsync(int id, DepartmentUpdateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;
            existing.DepartmentName = dto.DepartmentName;
            existing.Description = dto.Description;
            await _repo.UpdateAsync(existing);
            return new DepartmentReadDto
            {
                DepartmentId = existing.DepartmentId, DepartmentName = existing.DepartmentName, Description = existing.Description
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var department = await _repo.GetByIdAsync(id);
            if (department == null) return false;
            await _repo.DeleteAsync(department);
            return true;
        }
    }
}
