using AutoMapper;
using DepartmentService.DTOs;
using DepartmentService.Interfaces;
using DepartmentService.Models;

namespace DepartmentService.Services
{
    /// <summary>
    /// Optional service layer for DepartmentService.
    /// Wraps the repository and handles any future business logic
    /// (validation, enrichment, caching) without touching the controller.
    /// </summary>
    public class DepartmentBusinessService
    {
        private readonly IDepartmentRepository _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentBusinessService> _logger;

        public DepartmentBusinessService(
            IDepartmentRepository repo,
            IMapper mapper,
            ILogger<DepartmentBusinessService> logger)
        {
            _repo   = repo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<DepartmentReadDto>> GetAllAsync()
        {
            var departments = await _repo.GetAllAsync();
            return _mapper.Map<List<DepartmentReadDto>>(departments);
        }

        public async Task<DepartmentReadDto?> GetByIdAsync(int id)
        {
            var dept = await _repo.GetByIdAsync(id);
            return dept == null ? null : _mapper.Map<DepartmentReadDto>(dept);
        }

        public async Task<DepartmentReadDto> CreateAsync(DepartmentCreateDto dto)
        {
            var department = _mapper.Map<Department>(dto);
            department.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(department);
            _logger.LogInformation("Created department {Id} — {Name}", department.DepartmentId, department.Name);
            return _mapper.Map<DepartmentReadDto>(department);
        }

        public async Task<DepartmentReadDto?> UpdateAsync(int id, DepartmentCreateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;
            _mapper.Map(dto, existing);
            await _repo.UpdateAsync(existing);
            return _mapper.Map<DepartmentReadDto>(existing);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(existing);
            _logger.LogInformation("Deleted department {Id}", id);
            return true;
        }
    }
}
