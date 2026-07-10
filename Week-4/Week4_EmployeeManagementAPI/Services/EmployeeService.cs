using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Week3_EmployeeManagementAPI.DTOs;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>Business logic for Employee operations — updated with In-Memory Caching.</summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EmployeeService> _logger;

        private const string EmployeesVersionKey = "Employees_Version";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public EmployeeService(
            IEmployeeRepository repository,
            IMapper mapper,
            IMemoryCache cache,
            ILogger<EmployeeService> logger)
        {
            _repository = repository;
            _mapper     = mapper;
            _cache      = cache;
            _logger     = logger;
        }

        public async Task<List<EmployeeReadDto>> GetAllEmployeesAsync(EmployeeQueryParameters queryParams)
        {
            _logger.LogInformation("EmployeeService: GetAllEmployeesAsync with queryParams.");
            
            // Build cache key based on query parameters and cache version
            var version = GetEmployeesVersion();
            var cacheKey = $"Employees_List_v_{version}_p_{queryParams.PageNumber}_s_{queryParams.PageSize}_t_{queryParams.SearchTerm}_d_{queryParams.DepartmentId}_sb_{queryParams.SortBy}_a_{queryParams.IsAscending}";

            if (!_cache.TryGetValue(cacheKey, out List<EmployeeReadDto>? cachedList))
            {
                _logger.LogInformation("EmployeeService: Cache miss for key '{CacheKey}'. Fetching from database.", cacheKey);
                var employees = await _repository.GetAllAsync(queryParams);
                cachedList = _mapper.Map<List<EmployeeReadDto>>(employees);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheDuration)
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

                _cache.Set(cacheKey, cachedList, cacheEntryOptions);
            }
            else
            {
                _logger.LogInformation("EmployeeService: Cache hit for key '{CacheKey}'.", cacheKey);
            }

            return cachedList!;
        }

        public async Task<EmployeeReadDto?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation("EmployeeService: GetEmployeeByIdAsync ID {Id}", id);
            var cacheKey = $"Employee_{id}";

            if (!_cache.TryGetValue(cacheKey, out EmployeeReadDto? cachedEmployee))
            {
                _logger.LogInformation("EmployeeService: Cache miss for key '{CacheKey}'. Fetching from database.", cacheKey);
                var employee = await _repository.GetByIdAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("EmployeeService: Employee ID {Id} not found.", id);
                    return null;
                }
                cachedEmployee = _mapper.Map<EmployeeReadDto>(employee);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheDuration)
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

                _cache.Set(cacheKey, cachedEmployee, cacheEntryOptions);
            }
            else
            {
                _logger.LogInformation("EmployeeService: Cache hit for key '{CacheKey}'.", cacheKey);
            }

            return cachedEmployee;
        }

        public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto createDto)
        {
            _logger.LogInformation("EmployeeService: Creating {First} {Last}", createDto.FirstName, createDto.LastName);
            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeId = 0;
            await _repository.AddAsync(employee);
            await _repository.LoadDepartmentAsync(employee);
            
            // Invalidate cache
            InvalidateEmployeesCache();

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

            // Invalidate cache
            InvalidateEmployeesCache(id);

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

            // Invalidate cache
            InvalidateEmployeesCache(id);

            return true;
        }

        public async Task<bool> EmployeeExistsAsync(int id)
            => await _repository.ExistsAsync(id);

        #region Cache Helpers

        private string GetEmployeesVersion()
        {
            return _cache.GetOrCreate(EmployeesVersionKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return Guid.NewGuid().ToString();
            })!;
        }

        private void InvalidateEmployeesCache(int? employeeId = null)
        {
            _logger.LogInformation("EmployeeService: Invalidating cache. Version incremented.");
            _cache.Remove(EmployeesVersionKey);
            if (employeeId.HasValue)
            {
                _cache.Remove($"Employee_{employeeId.Value}");
            }
        }

        #endregion
    }
}
