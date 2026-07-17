using AutoMapper;
using DepartmentService.DTOs;
using DepartmentService.Interfaces;
using DepartmentService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DepartmentService.Controllers
{
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    [Produces("application/json")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentRepository _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentRepository repo, IMapper mapper,
            ILogger<DepartmentsController> logger)
        { _repo = repo; _mapper = mapper; _logger = logger; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repo.GetAllAsync();
            return Ok(new { Success = true, Data = _mapper.Map<List<DepartmentReadDto>>(list) });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var d = await _repo.GetByIdAsync(id);
            if (d == null) return NotFound(new { Success = false, Message = $"Department {id} not found." });
            return Ok(new { Success = true, Data = _mapper.Map<DepartmentReadDto>(d) });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentCreateDto dto)
        {
            var dept = _mapper.Map<Department>(dto);
            dept.CreatedAt = DateTime.UtcNow;
            await _repo.AddAsync(dept);
            return CreatedAtAction(nameof(GetById), new { id = dept.DepartmentId },
                new { Success = true, Data = _mapper.Map<DepartmentReadDto>(dept) });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentCreateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound(new { Success = false, Message = $"Department {id} not found." });
            _mapper.Map(dto, existing);
            await _repo.UpdateAsync(existing);
            return Ok(new { Success = true, Data = _mapper.Map<DepartmentReadDto>(existing) });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound(new { Success = false, Message = $"Department {id} not found." });
            await _repo.DeleteAsync(existing);
            return Ok(new { Success = true, Message = $"Department {id} deleted." });
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() =>
            Ok(new { Status = "Healthy", Service = "DepartmentService", Timestamp = DateTime.UtcNow });
    }
}
