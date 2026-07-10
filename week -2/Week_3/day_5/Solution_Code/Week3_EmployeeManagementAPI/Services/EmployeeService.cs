using AutoMapper;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Concrete implementation of IEmployeeService — Day 5 update.
    /// Added structured ILogger logging at Information, Warning, and Error levels.
    /// Exceptions propagate up to ExceptionMiddleware — no try/catch here.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IEmployeeRepository repository,
            IMapper mapper,
            ILogger<EmployeeService> logger)
        {
            _repository = repository;
            _mapper     = mapper;
            _logger     = logger;
        }

        // -----------------------------------------------------------------------
        // GET ALL
        // -----------------------------------------------------------------------
        public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync()
        {
            _logger.LogInformation("EmployeeService: Fetching all employees from repository.");

            var employees = await _repository.GetAllAsync();

            _logger.LogInformation("EmployeeService: Retrieved {Count} employees.", employees.Count);

            return _mapper.Map<List<EmployeeReadDto>>(employees);
        }

        // -----------------------------------------------------------------------
        // GET BY ID
        // -----------------------------------------------------------------------
        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation("EmployeeService: Fetching employee with ID {Id}.", id);

            var employee = await _repository.GetByIdAsync(id);

            if (employee == null)
            {
                _logger.LogWarning("EmployeeService: Employee with ID {Id} was not found.", id);
                return null;
            }

            _logger.LogInformation("EmployeeService: Found employee {FullName} (ID {Id}).",
                $"{employee.FirstName} {employee.LastName}", id);

            return _mapper.Map<EmployeeReadDto>(employee);
        }

        // -----------------------------------------------------------------------
        // CREATE
        // -----------------------------------------------------------------------
        public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto)
        {
            _logger.LogInformation("EmployeeService: Creating employee {FirstName} {LastName}.",
                createDto.FirstName, createDto.LastName);

            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;

            await _repository.AddAsync(employee);
            await _repository.LoadDepartmentAsync(employee);

            _logger.LogInformation("EmployeeService: Employee created with ID {Id}.", employee.EmployeeId);

            return _mapper.Map<EmployeeReadDto>(employee);
        }

        // -----------------------------------------------------------------------
        // UPDATE
        // -----------------------------------------------------------------------
        public async Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto)
        {
            _logger.LogInformation("EmployeeService: Updating employee with ID {Id}.", id);

            var existing = await _repository.GetByIdAsync(id);

            if (existing == null)
            {
                _logger.LogWarning("EmployeeService: Employee with ID {Id} not found for update.", id);
                return null;
            }

            _mapper.Map(updateDto, existing);
            existing.EmployeeId = id;

            await _repository.UpdateAsync(existing);
            await _repository.LoadDepartmentAsync(existing);

            _logger.LogInformation("EmployeeService: Employee with ID {Id} updated successfully.", id);

            return _mapper.Map<EmployeeReadDto>(existing);
        }

        // -----------------------------------------------------------------------
        // DELETE
        // -----------------------------------------------------------------------
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            _logger.LogInformation("EmployeeService: Deleting employee with ID {Id}.", id);

            var employee = await _repository.GetByIdAsync(id);

            if (employee == null)
            {
                _logger.LogWarning("EmployeeService: Employee with ID {Id} not found for deletion.", id);
                return false;
            }

            await _repository.DeleteAsync(employee);

            _logger.LogInformation("EmployeeService: Employee with ID {Id} deleted successfully.", id);

            return true;
        }

        // -----------------------------------------------------------------------
        // EXISTS CHECK
        // -----------------------------------------------------------------------
        public async Task<bool> EmployeeExistsAsync(int id)
        {
            var exists = await _repository.ExistsAsync(id);
            _logger.LogInformation("EmployeeService: Exists check for ID {Id} — {Result}.", id, exists);
            return exists;
        }
    }
}
