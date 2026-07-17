using EmployeeService.Data;
using EmployeeService.DTOs;
using EmployeeService.Interfaces;
using EmployeeService.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly EmployeeDbContext _context;

        public EmployeeRepository(EmployeeDbContext context) => _context = context;

        public async Task<List<Employee>> GetAllAsync(EmployeeQueryParameters q)
        {
            var query = _context.Employees.AsNoTracking().AsQueryable();

            if (q.DepartmentId.HasValue)
                query = query.Where(e => e.DepartmentId == q.DepartmentId.Value);

            if (!string.IsNullOrWhiteSpace(q.SearchTerm))
            {
                var term = q.SearchTerm.Trim().ToLower();
                query = query.Where(e => e.FirstName.ToLower().Contains(term)
                                      || e.LastName.ToLower().Contains(term)
                                      || e.Email.ToLower().Contains(term)
                                      || e.Position.ToLower().Contains(term));
            }

            query = q.SortBy.ToLower() switch
            {
                "firstname" => q.IsAscending ? query.OrderBy(e => e.FirstName) : query.OrderByDescending(e => e.FirstName),
                "lastname"  => q.IsAscending ? query.OrderBy(e => e.LastName)  : query.OrderByDescending(e => e.LastName),
                "salary"    => q.IsAscending ? query.OrderBy(e => e.Salary)    : query.OrderByDescending(e => e.Salary),
                "hiredate"  => q.IsAscending ? query.OrderBy(e => e.HireDate)  : query.OrderByDescending(e => e.HireDate),
                _           => query.OrderBy(e => e.EmployeeId)
            };

            return await query.Skip((q.PageNumber - 1) * q.PageSize).Take(q.PageSize).ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(int id) =>
            await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == id);

        public async Task AddAsync(Employee e)      { await _context.Employees.AddAsync(e); await _context.SaveChangesAsync(); }
        public async Task UpdateAsync(Employee e)   { _context.Employees.Update(e); await _context.SaveChangesAsync(); }
        public async Task DeleteAsync(Employee e)   { _context.Employees.Remove(e); await _context.SaveChangesAsync(); }
        public async Task<bool> ExistsAsync(int id) => await _context.Employees.AnyAsync(e => e.EmployeeId == id);
    }
}
