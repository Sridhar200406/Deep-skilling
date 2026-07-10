using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Concrete implementation of IEmployeeService.
    /// Uses AppDbContext (injected via constructor DI) to query SQL Server.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;

        // Constructor injection — ASP.NET Core DI provides AppDbContext automatically
        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            // Include() performs a SQL JOIN to load the related Department
            return await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()           // read-only — no change tracking overhead
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
        }
    }
}
