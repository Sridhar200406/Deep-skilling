using EmployeeService.Data;
using EmployeeService.DTOs;
using EmployeeService.Interfaces;
using EmployeeService.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly EmployeeDbContext _ctx;
        public EmployeeRepository(EmployeeDbContext ctx) => _ctx = ctx;

        public async Task<List<Employee>> GetAllAsync(EmployeeQueryParameters q)
        {
            var query = _ctx.Employees.AsNoTracking().AsQueryable();

            if (q.DepartmentId.HasValue)
                query = query.Where(e => e.DepartmentId == q.DepartmentId.Value);

            if (!string.IsNullOrWhiteSpace(q.SearchTerm))
            {
                var t = q.SearchTerm.Trim().ToLower();
                query = query.Where(e => e.FirstName.ToLower().Contains(t)
                                      || e.LastName.ToLower().Contains(t)
                                      || e.Email.ToLower().Contains(t)
                                      || e.Position.ToLower().Contains(t));
            }

            query = q.SortBy.ToLower() switch
            {
                "firstname" => q.IsAscending ? query.OrderBy(e => e.FirstName)  : query.OrderByDescending(e => e.FirstName),
                "lastname"  => q.IsAscending ? query.OrderBy(e => e.LastName)   : query.OrderByDescending(e => e.LastName),
                "salary"    => q.IsAscending ? query.OrderBy(e => e.Salary)     : query.OrderByDescending(e => e.Salary),
                "hiredate"  => q.IsAscending ? query.OrderBy(e => e.HireDate)   : query.OrderByDescending(e => e.HireDate),
                _           => query.OrderBy(e => e.EmployeeId)
            };

            return await query.Skip((q.PageNumber - 1) * q.PageSize).Take(q.PageSize).ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(int id) =>
            await _ctx.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == id);

        public async Task AddAsync(Employee e)    { await _ctx.Employees.AddAsync(e); await _ctx.SaveChangesAsync(); }
        public async Task UpdateAsync(Employee e) { _ctx.Employees.Update(e);         await _ctx.SaveChangesAsync(); }
        public async Task DeleteAsync(Employee e) { _ctx.Employees.Remove(e);         await _ctx.SaveChangesAsync(); }
        public async Task<bool> ExistsAsync(int id) => await _ctx.Employees.AnyAsync(e => e.EmployeeId == id);
    }
}
