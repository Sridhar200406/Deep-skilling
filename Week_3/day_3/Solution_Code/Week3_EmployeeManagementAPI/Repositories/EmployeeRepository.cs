using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Repositories
{
    /// <summary>
    /// Concrete implementation of IEmployeeRepository.
    /// This is the ONLY class in the project that is allowed to touch AppDbContext directly.
    /// All EF Core calls live here — nowhere else.
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;

        // AppDbContext is injected by the DI container (Scoped lifetime matches)
        public EmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        // -----------------------------------------------------------------------
        // READ ALL
        // Include() = SQL JOIN to load the related Department in one query.
        // AsNoTracking() = skip change-tracker for read-only queries (faster).
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<List<Employee>> GetAllAsync()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .ToListAsync();
        }

        // -----------------------------------------------------------------------
        // READ ONE
        // FirstOrDefaultAsync with a predicate is preferred over Find() here
        // because we need the Include() for the Department navigation property.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
        }

        // -----------------------------------------------------------------------
        // CREATE
        // AddAsync() stages the entity for INSERT.
        // SaveChangesAsync() executes the INSERT and populates EmployeeId.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task AddAsync(Employee employee)
        {
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();
        }

        // -----------------------------------------------------------------------
        // UPDATE
        // The entity passed in must already be tracked by this DbContext instance.
        // Update() marks all properties as Modified → generates full UPDATE SQL.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        // -----------------------------------------------------------------------
        // DELETE
        // Remove() marks the entity for DELETE.
        // SaveChangesAsync() executes the DELETE SQL.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task DeleteAsync(Employee employee)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }

        // -----------------------------------------------------------------------
        // EXISTS CHECK
        // AnyAsync() generates: SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END
        // — the most efficient existence check in SQL.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Employees
                .AnyAsync(e => e.EmployeeId == id);
        }

        // -----------------------------------------------------------------------
        // LOAD DEPARTMENT (post-write navigation reload)
        // After an Add or Update, the tracked entity's Department nav property
        // may be null. LoadAsync() fires a second SELECT to populate it so the
        // API response body includes the full department object.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task LoadDepartmentAsync(Employee employee)
        {
            await _context.Entry(employee)
                .Reference(e => e.Department)
                .LoadAsync();
        }
    }
}
