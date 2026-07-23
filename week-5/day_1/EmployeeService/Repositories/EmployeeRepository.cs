using Microsoft.EntityFrameworkCore;
using EmployeeService.Data;
using EmployeeService.DTOs;
using EmployeeService.Interfaces;
using EmployeeService.Models;

namespace EmployeeService.Repositories
{
    /// <summary>Concrete implementation of IEmployeeRepository with pagination, filtering, and sorting.</summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly EmployeeDbContext _context;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(EmployeeDbContext context, ILogger<EmployeeRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        public async Task<List<Employee>> GetAllAsync(EmployeeQueryParameters queryParams)
        {
            _logger.LogInformation("EmployeeRepository: GetAllAsync with parameters.");
            var query = _context.Employees.AsNoTracking().AsQueryable();

            // 1. Filtering by Department
            if (queryParams.DepartmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == queryParams.DepartmentId.Value);
            }

            // 2. Filtering by SearchTerm (FirstName, LastName, Email, Position)
            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                var term = queryParams.SearchTerm.Trim().ToLower();
                query = query.Where(e => e.FirstName.ToLower().Contains(term)
                                      || e.LastName.ToLower().Contains(term)
                                      || e.Email.ToLower().Contains(term)
                                      || e.Position.ToLower().Contains(term));
            }

            // 3. Sorting
            if (!string.IsNullOrWhiteSpace(queryParams.SortBy))
            {
                var sortBy = queryParams.SortBy.Trim().ToLower();
                query = sortBy switch
                {
                    "firstname" => queryParams.IsAscending ? query.OrderBy(e => e.FirstName) : query.OrderByDescending(e => e.FirstName),
                    "lastname"  => queryParams.IsAscending ? query.OrderBy(e => e.LastName)  : query.OrderByDescending(e => e.LastName),
                    "email"     => queryParams.IsAscending ? query.OrderBy(e => e.Email)     : query.OrderByDescending(e => e.Email),
                    "position"  => queryParams.IsAscending ? query.OrderBy(e => e.Position)  : query.OrderByDescending(e => e.Position),
                    "salary"    => queryParams.IsAscending ? query.OrderBy(e => e.Salary)    : query.OrderByDescending(e => e.Salary),
                    "hiredate"  => queryParams.IsAscending ? query.OrderBy(e => e.HireDate)  : query.OrderByDescending(e => e.HireDate),
                    _           => queryParams.IsAscending ? query.OrderBy(e => e.EmployeeId) : query.OrderByDescending(e => e.EmployeeId)
                };
            }
            else
            {
                query = query.OrderBy(e => e.EmployeeId);
            }

            // 4. Pagination
            var skip = (queryParams.PageNumber - 1) * queryParams.PageSize;
            var employees = await query.Skip(skip).Take(queryParams.PageSize).ToListAsync();

            _logger.LogInformation("EmployeeRepository: Returned {Count} records.", employees.Count);
            return employees;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            _logger.LogInformation("EmployeeRepository: GetByIdAsync ID {Id}.", id);
            var employee = await _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee == null) _logger.LogWarning("EmployeeRepository: ID {Id} not found.", id);
            return employee;
        }

        public async Task AddAsync(Employee employee)
        {
            _logger.LogInformation("EmployeeRepository: AddAsync {First} {Last}.", employee.FirstName, employee.LastName);
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();
            _logger.LogInformation("EmployeeRepository: Inserted ID {Id}.", employee.EmployeeId);
        }

        public async Task UpdateAsync(Employee employee)
        {
            _logger.LogInformation("EmployeeRepository: UpdateAsync ID {Id}.", employee.EmployeeId);
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Employee employee)
        {
            _logger.LogInformation("EmployeeRepository: DeleteAsync ID {Id}.", employee.EmployeeId);
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.EmployeeId == id);
        }
    }
}
