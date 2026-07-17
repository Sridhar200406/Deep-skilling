using AutoMapper;
using DepartmentService.Data;
using DepartmentService.DTOs;
using DepartmentService.Interfaces;
using DepartmentService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentService.Controllers
{
    /// <summary>
    /// Department CRUD API.
    /// All endpoints require a valid JWT (enforced at the API Gateway level,
    /// but we also enforce here for direct calls).
    /// </summary>
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    [Produces("application/json")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentRepository _repo;
        private readonly IMapper               _mapper;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentRepository repo, IMapper mapper,
            ILogger<DepartmentsController> logger)
        {
            _repo   = repo;
            _mapper = mapper;
            _logger = logger;
        }

        // GET /api/departments
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _repo.GetAllAsync();
            return Ok(new { Success = true, Data = _mapper.Map<List<DepartmentReadDto>>(departments) });
        }

        // GET /api/departments/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dept = await _repo.GetByIdAsync(id);
            if (dept == null)
                return NotFound(new { Success = false, Message = $"Department {id} not found." });

            return Ok(new { Success = true, Data = _mapper.Map<DepartmentReadDto>(dept) });
        }

        // POST /api/departments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateDto dto)
        {
            var department = _mapper.Map<Department>(dto);
            department.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(department);

            _logger.LogInformation("Created department {Id} — {Name}", department.DepartmentId, department.Name);
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
            _logger.LogInformation("Deleted department {Id}", id);
            return Ok(new { Success = true, Message = $"Department {id} deleted." });
        }

        // GET /api/departments/health
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() =>
            Ok(new { Status = "Healthy", Service = "DepartmentService", Timestamp = DateTime.UtcNow });
    }
}
