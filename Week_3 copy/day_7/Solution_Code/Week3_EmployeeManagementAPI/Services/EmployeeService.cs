using AutoMapper;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>Business logic for Employee operations — unchanged from Day 5/6.</summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(IEmployeeRepository repository, IMapper mapper, ILogger<EmployeeService> logger)
        {
            _repository = repository;
            _mapper     = mapper;
            _logger     = logger;
        }

        public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync()
        {
            _logger.LogInformation("EmployeeService: GetAllEmployeesAsync");
            var employees = await _repository.GetAllAsync();
            _logger.LogInformation("EmployeeService: Retrieved {Count} employees.", employees.Count);
            return _mapper.Map<List<EmployeeReadDto>>(employees);
        }

        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation("EmployeeService: GetEmployeeByIdAsync ID {Id}", id);
            var employee = await _repository.GetByIdAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("EmployeeService: Employee ID {Id} not found.", id);
                return null;
            }
            return _mapper.Map<EmployeeReadDto>(employee);
        }

        public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto)
        {
            _logger.LogInformation("EmployeeService: Creating {First} {Last}", createDto.FirstName, createDto.LastName);
            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;
            await _repository.AddAsync(employee);
            await _repository.LoadDepartmentAsync(employee);
            _logger.LogInformation("EmployeeService: Created ID {Id}", employee.EmployeeId);
            return _mapper.Map<EmployeeReadDto>(employee);
        }

        public async Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto)
        {
            _logger.LogInformation("EmployeeService: Updating ID {Id}", id);
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("EmployeeService: ID {Id} not found for update.", id);
                return null;
            }
            _mapper.Map(updateDto, existing);
            existing.EmployeeId = id;
            await _repository.UpdateAsync(existing);
            await _repository.LoadDepartmentAsync(existing);
            return _mapper.Map<EmployeeReadDto>(existing);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            _logger.LogInformation("EmployeeService: Deleting ID {Id}", id);
            var employee = await _repository.GetByIdAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("EmployeeService: ID {Id} not found for deletion.", id);
                return false;
            }
            await _repository.DeleteAsync(employee);
            return true;
        }

        public async Task<bool> EmployeeExistsAsync(int id)
            => await _repository.ExistsAsync(id);
    }
}
