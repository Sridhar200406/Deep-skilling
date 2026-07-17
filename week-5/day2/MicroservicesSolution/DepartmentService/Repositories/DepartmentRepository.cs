using DepartmentService.Data;
using DepartmentService.Interfaces;
using DepartmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DepartmentService.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly DepartmentDbContext _context;

        public DepartmentRepository(DepartmentDbContext context) => _context = context;

        public async Task<List<Department>> GetAllAsync() =>
            await _context.Departments.AsNoTracking().ToListAsync();

        public async Task<Department?> GetByIdAsync(int id) =>
            await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.DepartmentId == id);

        public async Task AddAsync(Department department)
        {
            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Department department)
        {
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Department department)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Departments.AnyAsync(d => d.DepartmentId == id);
    }
}
