using AutoMapper;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Concrete implementation of IEmployeeService — Day 4 update.
    /// 
    /// New responsibilities vs Day 3:
    ///  - Accepts DTOs from the controller (not Employee entities).
    ///  - Uses AutoMapper to convert DTO → Employee before calling the repository.
    ///  - Uses AutoMapper to convert Employee → EmployeeReadDto before returning to the controller.
    /// 
    /// Dependency chain:
    ///   Controller → IEmployeeService (DTOs) → IEmployeeRepository (entities) → DbContext → SQL Server
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;

        // Both IEmployeeRepository and IMapper are injected by DI
        public EmployeeService(IEmployeeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // -----------------------------------------------------------------------
        // GET ALL — returns List<EmployeeReadDto>
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync()
        {
            var employees = await _repository.GetAllAsync();
            // Map each Employee entity to EmployeeReadDto
            // AutoMapper handles FullName and DepartmentName via EmployeeProfile
            return _mapper.Map<List<EmployeeReadDto>>(employees);
        }

        // -----------------------------------------------------------------------
        // GET BY ID — returns EmployeeReadDto?
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            var employee = await _repository.GetByIdAsync(id);
            if (employee == null) return null;
            return _mapper.Map<EmployeeReadDto>(employee);
        }

        // -----------------------------------------------------------------------
        // CREATE — accepts EmployeeCreateDto, returns EmployeeReadDto
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto)
        {
            // Map DTO → entity (EmployeeId stays 0 — DB assigns it)
            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;

            await _repository.AddAsync(employee);
            await _repository.LoadDepartmentAsync(employee);

            // Map entity → read DTO for the response
            return _mapper.Map<EmployeeReadDto>(employee);
        }

        // -----------------------------------------------------------------------
        // UPDATE — accepts EmployeeUpdateDto, returns EmployeeReadDto?
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            // Map updated DTO fields onto the existing entity
            // AutoMapper overwrites every mapped property — EmployeeId stays correct
            _mapper.Map(updateDto, existing);
            existing.EmployeeId = id; // ensure ID is never overwritten by client

            await _repository.UpdateAsync(existing);
            await _repository.LoadDepartmentAsync(existing);

            return _mapper.Map<EmployeeReadDto>(existing);
        }

        // -----------------------------------------------------------------------
        // DELETE — unchanged logic, no DTOs needed
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _repository.GetByIdAsync(id);
            if (employee == null) return false;

            await _repository.DeleteAsync(employee);
            return true;
        }

        // -----------------------------------------------------------------------
        // EXISTS CHECK
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<bool> EmployeeExistsAsync(int id)
        {
            return await _repository.ExistsAsync(id);
        }
    }
}
