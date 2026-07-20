using DepartmentService.Data;
using DepartmentService.Interfaces;
using DepartmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DepartmentService.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly DepartmentDbContext _ctx;
        public DepartmentRepository(DepartmentDbContext ctx) => _ctx = ctx;

        public async Task<List<Department>> GetAllAsync() =>
            await _ctx.Departments.AsNoTracking().ToListAsync();
        public async Task<Department?> GetByIdAsync(int id) =>
            await _ctx.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.DepartmentId == id);
        public async Task AddAsync(Department d)    { await _ctx.Departments.AddAsync(d); await _ctx.SaveChangesAsync(); }
        public async Task UpdateAsync(Department d) { _ctx.Departments.Update(d);         await _ctx.SaveChangesAsync(); }
        public async Task DeleteAsync(Department d) { _ctx.Departments.Remove(d);         await _ctx.SaveChangesAsync(); }
        public async Task<bool> ExistsAsync(int id) => await _ctx.Departments.AnyAsync(d => d.DepartmentId == id);
    }
}
