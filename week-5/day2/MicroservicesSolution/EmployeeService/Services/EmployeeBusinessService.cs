using AutoMapper;
using EmployeeService.DTOs;
using EmployeeService.HttpClients;
using EmployeeService.Interfaces;
using EmployeeService.Models;

namespace EmployeeService.Services
{
    /// <summary>
    /// Employee business logic.
    /// Day 2 addition: enriches EmployeeReadDto.DepartmentName by calling
    /// DepartmentService via the injected typed HttpClient.
    /// </summary>
    public class EmployeeBusinessService : IEmployeeService
    {
        private readonly IEmployeeRepository      _repo;
        private readonly IMapper                  _mapper;
        private readonly DepartmentServiceClient  _deptClient;
        private readonly ILogger<EmployeeBusinessService> _logger;

        public EmployeeBusinessService(
            IEmployeeRepository     repo,
            IMapper                 mapper,
            DepartmentServiceClient deptClient,
            ILogger<EmployeeBusinessService> logger)
        {
            _repo       = repo;
            _mapper     = mapper;
            _deptClient = deptClient;
            _logger     = logger;
        }

        public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync(EmployeeQueryParameters q)
        {
            var employees = await _repo.GetAllAsync(q);
            var dtos = _mapper.Map<List<EmployeeReadDto>>(employees);

            // Batch-enrich department names (one call per unique dept id)
            var uniqueDeptIds = dtos.Select(d => d.DepartmentId).Distinct().ToList();
            var deptCache = new Dictionary<int, string?>();
            foreach (var id in uniqueDeptIds)
            {
                var dept = await _deptClient.GetDepartmentByIdAsync(id);
                deptCache[id] = dept?.Name;
            }

            foreach (var dto in dtos)
                dto.DepartmentName = deptCache.GetValueOrDefault(dto.DepartmentId);

            return dtos;
        }

        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            var employee = await _repo.GetByIdAsync(id);
            if (employee == null) return null;

            var dto  = _mapper.Map<EmployeeReadDto>(employee);
            var dept = await _deptClient.GetDepartmentByIdAsync(employee.DepartmentId);
            dto.DepartmentName = dept?.Name;
            return dto;
        }

        public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto)
        {
            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;
            await _repo.AddAsync(employee);
            _logger.LogInformation("Created employee ID {Id}", employee.EmployeeId);
            return _mapper.Map<EmployeeReadDto>(employee);
        }

        public async Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;
            _mapper.Map(updateDto, existing);
            existing.EmployeeId = id;
            await _repo.UpdateAsync(existing);
            return _mapper.Map<EmployeeReadDto>(existing);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _repo.GetByIdAsync(id);
            if (employee == null) return false;
            await _repo.DeleteAsync(employee);
            return true;
        }
    }
}
