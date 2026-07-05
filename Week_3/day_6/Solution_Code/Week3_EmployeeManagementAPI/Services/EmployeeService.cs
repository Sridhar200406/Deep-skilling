using AutoMapper;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>Concrete implementation of IEmployeeService — unchanged from Day 5.</summary>
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
            _logger.LogInformation("EmployeeService: Fetching all employees.");
            var employees = await _repository.GetAllAsync();
            _logger.LogInformation("EmployeeService: Retrieved {Count} employees.", employees.Count);
            return _mapper.Map<List<EmployeeReadDto>>(employees);
        }

        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation("EmployeeService: Fetching employee ID {Id}.", id);
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
            _logger.LogInformation("EmployeeService: Creating employee {First} {Last}.", createDto.FirstName, createDto.LastName);
            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;
            await _repository.AddAsync(employee);
            await _repository.LoadDepartmentAsync(employee);
            _logger.LogInformation("EmployeeService: Created employee ID {Id}.", employee.EmployeeId);
            return _mapper.Map<EmployeeReadDto>(employee);
        }

        public async Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto)
        {
            _logger.LogInformation("EmployeeService: Updating employee ID {Id}.", id);
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("EmployeeService: Employee ID {Id} not found for update.", id);
                return null;
            }
            _mapper.Map(updateDto, existing);
            existing.EmployeeId = id;
            await _repository.UpdateAsync(existing);
            await _repository.LoadDepartmentAsync(existing);
            _logger.LogInformation("EmployeeService: Employee ID {Id} updated.", id);
            return _mapper.Map<EmployeeReadDto>(existing);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            _logger.LogInformation("EmployeeService: Deleting employee ID {Id}.", id);
            var employee = await _repository.GetByIdAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("EmployeeService: Employee ID {Id} not found for deletion.", id);
                return false;
            }
            await _repository.DeleteAsync(employee);
            _logger.LogInformation("EmployeeService: Employee ID {Id} deleted.", id);
            return true;
        }

        public async Task<bool> EmployeeExistsAsync(int id)
        {
            return await _repository.ExistsAsync(id);
        }
    }
}
