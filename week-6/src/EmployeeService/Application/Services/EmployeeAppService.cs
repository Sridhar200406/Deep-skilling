using EmployeeService.Application.DTOs;
using EmployeeService.Domain.Entities;
using EmployeeService.Infrastructure.Data;
using EmployeeService.Infrastructure.Functions;
using EmployeeService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Text.Json;

namespace EmployeeService.Application.Services;

public interface IEmployeeAppService
{
    Task<PagedResult<EmployeeListDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
    Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> DepartmentExistsAsync(int departmentId);
}

public class EmployeeAppService : IEmployeeAppService
{
    private readonly EmployeeDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<EmployeeAppService> _logger;
    private readonly IFunctionTriggerService? _functionTrigger;
    private readonly IServiceBusPublisher? _serviceBusPublisher;
    private const string CacheKeyPrefix = "employee:";

    public EmployeeAppService(
        EmployeeDbContext context,
        IDistributedCache cache,
        ILogger<EmployeeAppService> logger,
        IFunctionTriggerService? functionTrigger = null,
        IServiceBusPublisher? serviceBusPublisher = null)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _functionTrigger = functionTrigger;
        _serviceBusPublisher = serviceBusPublisher;
    }

    public async Task<PagedResult<EmployeeListDto>> GetAllAsync(int page, int pageSize, string? search)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Employees
            .Include(e => e.Department)
            .Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(search) ||
                e.LastName.ToLower().Contains(search) ||
                e.Email.ToLower().Contains(search) ||
                e.Position.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListDto
            {
                Id = e.Id,
                FullName = $"{e.FirstName} {e.LastName}",
                Email = e.Email,
                Position = e.Position,
                DepartmentName = e.Department != null ? e.Department.Name : "N/A",
                IsActive = e.IsActive
            })
            .ToListAsync();

        return new PagedResult<EmployeeListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        // Try cache first
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for employee {EmployeeId}", id);
            return JsonSerializer.Deserialize<EmployeeDto>(cached);
        }

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        if (employee == null) return null;

        var dto = MapToDto(employee);

        // Cache for 5 minutes
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        return dto;
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        // Check for duplicate email
        if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
            throw new InvalidOperationException($"An employee with email '{dto.Email}' already exists.");

        // Validate department
        if (!await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId && d.IsActive))
            throw new InvalidOperationException($"Department with ID {dto.DepartmentId} does not exist or is inactive.");

        var employee = new Employee
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            Position = dto.Position.Trim(),
            Salary = dto.Salary,
            HireDate = dto.HireDate,
            DepartmentId = dto.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created employee {EmployeeId}: {FullName}", employee.Id, $"{employee.FirstName} {employee.LastName}");

        // Reload with department
        await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        var dto = MapToDto(employee);

        // Fire-and-forget: trigger Azure Function notification (non-blocking)
        if (_functionTrigger != null)
        {
            _ = _functionTrigger.TriggerEmployeeNotificationAsync(
                employee.Id, employee.FirstName, employee.LastName,
                employee.Email, employee.Position,
                dto.DepartmentName, "EmployeeCreated");
        }

        return dto;
    }

    public async Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || !employee.IsActive) return null;

        // Check email uniqueness (exclude current)
        if (await _context.Employees.AnyAsync(e => e.Email == dto.Email && e.Id != id))
            throw new InvalidOperationException($"Email '{dto.Email}' is already used by another employee.");

        // Validate department
        if (!await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId && d.IsActive))
            throw new InvalidOperationException($"Department with ID {dto.DepartmentId} does not exist or is inactive.");

        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.Email = dto.Email.Trim().ToLower();
        employee.Phone = dto.Phone.Trim();
        employee.Position = dto.Position.Trim();
        employee.Salary = dto.Salary;
        employee.IsActive = dto.IsActive;
        employee.DepartmentId = dto.DepartmentId;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate cache
        await _cache.RemoveAsync($"{CacheKeyPrefix}{id}");
        _logger.LogInformation("Updated employee {EmployeeId}", id);

        await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        var dto = MapToDto(employee);

        // Fire-and-forget: trigger Azure Function notification (non-blocking)
        if (_functionTrigger != null)
        {
            _ = _functionTrigger.TriggerEmployeeNotificationAsync(
                employee.Id, employee.FirstName, employee.LastName,
                employee.Email, employee.Position,
                dto.DepartmentName, "EmployeeUpdated");
        }

        return dto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return false;

        // Soft delete
        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Invalidate cache
        await _cache.RemoveAsync($"{CacheKeyPrefix}{id}");
        _logger.LogInformation("Soft-deleted employee {EmployeeId}", id);

        return true;
    }

    public async Task<bool> DepartmentExistsAsync(int departmentId)
        => await _context.Departments.AnyAsync(d => d.Id == departmentId && d.IsActive);

    private static EmployeeDto MapToDto(Employee e) => new()
    {
        Id = e.Id,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Email = e.Email,
        Phone = e.Phone,
        Position = e.Position,
        Salary = e.Salary,
        HireDate = e.HireDate,
        IsActive = e.IsActive,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department?.Name ?? "N/A",
        ProfilePictureUrl = e.ProfilePictureUrl,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
