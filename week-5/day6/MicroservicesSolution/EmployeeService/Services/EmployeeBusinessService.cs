using AutoMapper;
using Caching;
using EmployeeService.DTOs;
using EmployeeService.HttpClients;
using EmployeeService.Interfaces;
using EmployeeService.Messaging;
using EmployeeService.Models;
using Shared.Events;

namespace EmployeeService.Services
{
    /// <summary>
    /// Employee business logic — Day 4 update.
    /// Adds Redis distributed caching:
    ///   GET  → check Redis first; on miss load from DB and populate Redis
    ///   POST/PUT/DELETE → write to DB, publish RabbitMQ event, then invalidate Redis
    /// </summary>
    public class EmployeeBusinessService : IEmployeeService
    {
        private readonly IEmployeeRepository    _repo;
        private readonly IMapper                _mapper;
        private readonly DepartmentServiceClient _deptClient;
        private readonly EmployeeEventPublisher  _publisher;
        private readonly IRedisCacheService      _cache;
        private readonly IConfiguration          _config;
        private readonly ILogger<EmployeeBusinessService> _logger;

        public EmployeeBusinessService(
            IEmployeeRepository    repo,
            IMapper                mapper,
            DepartmentServiceClient deptClient,
            EmployeeEventPublisher  publisher,
            IRedisCacheService      cache,
            IConfiguration          config,
            ILogger<EmployeeBusinessService> logger)
        {
            _repo       = repo;
            _mapper     = mapper;
            _deptClient = deptClient;
            _publisher  = publisher;
            _cache      = cache;
            _config     = config;
            _logger     = logger;
        }

        // ── READ ALL ─────────────────────────────────────────────────────────
        public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync(EmployeeQueryParameters q)
        {
            // Build a deterministic cache key from query params
            var cacheKey = CacheKeys.EmployeeList(
                $"p{q.PageNumber}s{q.PageSize}q{q.SearchTerm}d{q.DepartmentId}sb{q.SortBy}a{q.IsAscending}");

            var cached = await _cache.GetAsync<List<EmployeeReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Cache HIT  employees list key={Key}", cacheKey);
                return cached;
            }

            _logger.LogInformation("Cache MISS employees list key={Key} — loading from DB", cacheKey);

            var employees = await _repo.GetAllAsync(q);
            var dtos      = _mapper.Map<List<EmployeeReadDto>>(employees);

            // Enrich DepartmentName via typed HttpClient (with Polly resilience)
            var deptIds = dtos.Select(d => d.DepartmentId).Distinct();
            var deptCache = new Dictionary<int, string?>();
            foreach (var id in deptIds)
            {
                var dept = await _deptClient.GetDepartmentByIdAsync(id);
                deptCache[id] = dept?.Name;
            }
            foreach (var dto in dtos)
                dto.DepartmentName = deptCache.GetValueOrDefault(dto.DepartmentId);

            var expiry = TimeSpan.FromMinutes(
                _config.GetValue<int>("Cache:EmployeeExpiryMinutes", 10));
            await _cache.SetAsync(cacheKey, dtos, expiry);

            return dtos;
        }

        // ── READ BY ID ───────────────────────────────────────────────────────
        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            var cacheKey = CacheKeys.Employee(id);

            var cached = await _cache.GetAsync<EmployeeReadDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Cache HIT  employee id={Id}", id);
                return cached;
            }

            _logger.LogInformation("Cache MISS employee id={Id} — loading from DB", id);

            var employee = await _repo.GetByIdAsync(id);
            if (employee == null) return null;

            var dto  = _mapper.Map<EmployeeReadDto>(employee);
            var dept = await _deptClient.GetDepartmentByIdAsync(employee.DepartmentId);
            dto.DepartmentName = dept?.Name;

            var expiry = TimeSpan.FromMinutes(
                _config.GetValue<int>("Cache:EmployeeExpiryMinutes", 10));
            await _cache.SetAsync(cacheKey, dto, expiry);

            return dto;
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto)
        {
            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;
            await _repo.AddAsync(employee);

            // Publish domain event
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

            // Invalidate list cache (new record → all lists stale)
            await _cache.RemoveAsync(CacheKeys.EmployeeListAll());
            _logger.LogInformation("Cache INVALIDATED employee lists after CREATE id={Id}", employee.EmployeeId);

            return _mapper.Map<EmployeeReadDto>(employee);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<EmployeeReadDto?> UpdateEmployeeAsync(int id, EmployeeUpdateDto updateDto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return null;

            var oldDeptId = existing.DepartmentId;
            _mapper.Map(updateDto, existing);
            existing.EmployeeId = id;
            await _repo.UpdateAsync(existing);

            // Publish domain event
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

            // Invalidate this employee + all list caches
            await _cache.RemoveManyAsync(new[]
            {
                CacheKeys.Employee(id),
                CacheKeys.EmployeeListAll()
            });
            _logger.LogInformation("Cache INVALIDATED employee id={Id} after UPDATE", id);

            return _mapper.Map<EmployeeReadDto>(existing);
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _repo.GetByIdAsync(id);
            if (employee == null) return false;

            await _repo.DeleteAsync(employee);

            // Publish domain event
            _publisher.PublishEmployeeDeleted(new EmployeeDeletedEvent
            {
                EmployeeId   = employee.EmployeeId,
                Email        = employee.Email,
                DepartmentId = employee.DepartmentId
            });

            // Invalidate both specific and list cache
            await _cache.RemoveManyAsync(new[]
            {
                CacheKeys.Employee(id),
                CacheKeys.EmployeeListAll()
            });
            _logger.LogInformation("Cache INVALIDATED employee id={Id} after DELETE", id);

            return true;
        }
    }
}
