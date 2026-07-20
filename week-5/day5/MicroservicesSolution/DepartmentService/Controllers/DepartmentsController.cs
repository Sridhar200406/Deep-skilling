using AutoMapper;
using Caching;
using DepartmentService.DTOs;
using DepartmentService.Interfaces;
using DepartmentService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentService.Controllers
{
    /// <summary>
    /// Department CRUD API — Day 4: Redis caching on read, invalidation on write.
    /// </summary>
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    [Produces("application/json")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentRepository _repo;
        private readonly IMapper               _mapper;
        private readonly IRedisCacheService    _cache;
        private readonly IConfiguration        _config;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentRepository repo, IMapper mapper,
            IRedisCacheService cache, IConfiguration config,
            ILogger<DepartmentsController> logger)
        {
            _repo   = repo;
            _mapper = mapper;
            _cache  = cache;
            _config = config;
            _logger = logger;
        }

        // GET /api/departments
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cacheKey = CacheKeys.DepartmentListAll();
            var cached   = await _cache.GetAsync<List<DepartmentReadDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Cache HIT  departments list");
                return Ok(new { Success = true, Data = cached });
            }

            _logger.LogInformation("Cache MISS departments list — loading from DB");
            var list  = await _repo.GetAllAsync();
            var dtos  = _mapper.Map<List<DepartmentReadDto>>(list);
            var expiry = TimeSpan.FromMinutes(_config.GetValue<int>("Cache:DepartmentExpiryMinutes", 30));
            await _cache.SetAsync(cacheKey, dtos, expiry);

            return Ok(new { Success = true, Data = dtos });
        }

        // GET /api/departments/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cacheKey = CacheKeys.Department(id);
            var cached   = await _cache.GetAsync<DepartmentReadDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Cache HIT  department id={Id}", id);
                return Ok(new { Success = true, Data = cached });
            }

            _logger.LogInformation("Cache MISS department id={Id} — loading from DB", id);
            var dept = await _repo.GetByIdAsync(id);
            if (dept == null)
                return NotFound(new { Success = false, Message = $"Department {id} not found." });

            var dto    = _mapper.Map<DepartmentReadDto>(dept);
            var expiry = TimeSpan.FromMinutes(_config.GetValue<int>("Cache:DepartmentExpiryMinutes", 30));
            await _cache.SetAsync(cacheKey, dto, expiry);

            return Ok(new { Success = true, Data = dto });
        }

        // POST /api/departments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateDto dto)
        {
            var department = _mapper.Map<Department>(dto);
            department.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(department);

            await _cache.RemoveAsync(CacheKeys.DepartmentListAll());
            _logger.LogInformation("Cache INVALIDATED department list after CREATE id={Id}", department.DepartmentId);

            return CreatedAtAction(nameof(GetById), new { id = department.DepartmentId },
                new { Success = true, Data = _mapper.Map<DepartmentReadDto>(department) });
        }

        // PUT /api/departments/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentCreateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { Success = false, Message = $"Department {id} not found." });

            _mapper.Map(dto, existing);
            await _repo.UpdateAsync(existing);

            await _cache.RemoveManyAsync(new[] { CacheKeys.Department(id), CacheKeys.DepartmentListAll() });
            _logger.LogInformation("Cache INVALIDATED department id={Id} after UPDATE", id);

            return Ok(new { Success = true, Data = _mapper.Map<DepartmentReadDto>(existing) });
        }

        // DELETE /api/departments/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { Success = false, Message = $"Department {id} not found." });

            await _repo.DeleteAsync(existing);

            await _cache.RemoveManyAsync(new[] { CacheKeys.Department(id), CacheKeys.DepartmentListAll() });
            _logger.LogInformation("Cache INVALIDATED department id={Id} after DELETE", id);

            return Ok(new { Success = true, Message = $"Department {id} deleted." });
        }

        // GET /api/departments/health  (anonymous)
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() =>
            Ok(new { Status = "Healthy", Service = "DepartmentService", Timestamp = DateTime.UtcNow });
    }
}
