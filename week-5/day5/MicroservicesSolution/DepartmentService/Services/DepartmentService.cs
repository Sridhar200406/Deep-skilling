using AutoMapper;
using DepartmentService.DTOs;
using DepartmentService.Interfaces;
using DepartmentService.Models;

namespace DepartmentService.Services
{
    public class DepartmentBusinessService
    {
        private readonly IDepartmentRepository _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentBusinessService> _logger;

        public DepartmentBusinessService(IDepartmentRepository repo, IMapper mapper,
            ILogger<DepartmentBusinessService> logger)
        { _repo = repo; _mapper = mapper; _logger = logger; }

        public async Task<List<DepartmentReadDto>> GetAllAsync() =>
            _mapper.Map<List<DepartmentReadDto>>(await _repo.GetAllAsync());

        public async Task<DepartmentReadDto?> GetByIdAsync(int id)
        {
            var d = await _repo.GetByIdAsync(id);
            return d == null ? null : _mapper.Map<DepartmentReadDto>(d);
        }

        public async Task<DepartmentReadDto> CreateAsync(DepartmentCreateDto dto)
        {
            var dept = _mapper.Map<Department>(dto);
            dept.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(dept);
            _logger.LogInformation("Created department {Id} — {Name}", dept.DepartmentId, dept.Name);
            return _mapper.Map<DepartmentReadDto>(dept);
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
            return true;
        }
    }
}
