using AutoMapper;
using EmployeeService.DTOs;
using EmployeeService.HttpClients;
using EmployeeService.Interfaces;
using EmployeeService.Messaging;
using EmployeeService.Models;
using Shared.Events;

namespace EmployeeService.Services
{
    /// <summary>
    /// Employee business logic.
    /// Day 3: publishes domain events to RabbitMQ after every state change.
    /// </summary>
    public class EmployeeBusinessService : IEmployeeService
    {
        private readonly IEmployeeRepository     _repo;
        private readonly IMapper                 _mapper;
        private readonly DepartmentServiceClient _deptClient;
        private readonly EmployeeEventPublisher  _publisher;
        private readonly ILogger<EmployeeBusinessService> _logger;

        public EmployeeBusinessService(
            IEmployeeRepository     repo,
            IMapper                 mapper,
            DepartmentServiceClient deptClient,
            EmployeeEventPublisher  publisher,
            ILogger<EmployeeBusinessService> logger)
        { _repo = repo; _mapper = mapper; _deptClient = deptClient; _publisher = publisher; _logger = logger; }

        public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync(EmployeeQueryParameters q)
        {
            var employees = await _repo.GetAllAsync(q);
            var dtos = _mapper.Map<List<EmployeeReadDto>>(employees);
            var deptIds = dtos.Select(d => d.DepartmentId).Distinct();
            var cache = new Dictionary<int, string?>();
            foreach (var id in deptIds)
            {
                var d = await _deptClient.GetDepartmentByIdAsync(id);
                cache[id] = d?.Name;
            }
            foreach (var dto in dtos) dto.DepartmentName = cache.GetValueOrDefault(dto.DepartmentId);
            return dtos;
        }

        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;
            var dto  = _mapper.Map<EmployeeReadDto>(e);
            var dept = await _deptClient.GetDepartmentByIdAsync(e.DepartmentId);
            dto.DepartmentName = dept?.Name;
            return dto;
        }

        public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto)
        {
            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;
            await _repo.AddAsync(employee);

            // ── Publish event ────────────────────────────────────────────────
            _publisher.PublishEmployeeCreated(new EmployeeCreatedEvent
            {
                EmployeeId   = employee.EmployeeId,
                FirstName    = employee.FirstName,
                LastName     = employee.LastName,
                Email        = employee.Email,
                Position     = employee.Position,
                Salary       = employee.Salary,
                DepartmentId = employee.DepartmentId,
                HireDate     = employee.HireDate
            });

            _logger.LogInformation("Created employee ID {Id}", employee.EmployeeId);
            return _mapper.Map<EmployeeReadDto>(employee);
        }

        public async Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            var oldDeptId = existing.DepartmentId;
            _mapper.Map(updateDto, existing);
            existing.EmployeeId = id;
            await _repo.UpdateAsync(existing);

            // ── Publish event ────────────────────────────────────────────────
            _publisher.PublishEmployeeUpdated(new EmployeeUpdatedEvent
            {
                EmployeeId      = existing.EmployeeId,
                FirstName       = existing.FirstName,
                LastName        = existing.LastName,
                Email           = existing.Email,
                Position        = existing.Position,
                Salary          = existing.Salary,
                DepartmentId    = existing.DepartmentId,
                OldDepartmentId = oldDeptId != existing.DepartmentId ? oldDeptId : null,
                IsActive        = existing.IsActive
            });

            return _mapper.Map<EmployeeReadDto>(existing);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _repo.GetByIdAsync(id);
            if (employee == null) return false;
            await _repo.DeleteAsync(employee);

            // ── Publish event ────────────────────────────────────────────────
            _publisher.PublishEmployeeDeleted(new EmployeeDeletedEvent
            {
                EmployeeId   = employee.EmployeeId,
                Email        = employee.Email,
                DepartmentId = employee.DepartmentId
            });

            return true;
        }
    }
}
