using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Concrete implementation of IEmployeeService.
    /// Uses AppDbContext (injected via DI) to perform CRUD operations against SQL Server.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        // -----------------------------------------------------------------------
        // GET ALL
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .ToListAsync();
        }

        // -----------------------------------------------------------------------
        // GET BY ID
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
        }

        // -----------------------------------------------------------------------
        // CREATE
        // EF Core adds the entity and auto-generates the EmployeeId (IDENTITY column).
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            // Ensure the ID is not set by the client — DB generates it
            employee.EmployeeId = 0;

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Reload with Department navigation for the response body
            await _context.Entry(employee)
                .Reference(e => e.Department)
                .LoadAsync();

            return employee;
        }

        // -----------------------------------------------------------------------
        // UPDATE
        // Loads the existing record, applies field-by-field updates, saves.
        // This prevents overwriting fields the client did not send.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<Employee?> UpdateEmployeeAsync(int id, Employee employee)
        {
            var existing = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (existing == null)
                return null;

            // Apply only the updatable fields — EmployeeId is never changed
            existing.FirstName    = employee.FirstName;
            existing.LastName     = employee.LastName;
            existing.Email        = employee.Email;
            existing.Phone        = employee.Phone;
            existing.Position     = employee.Position;
            existing.Salary       = employee.Salary;
            existing.HireDate     = employee.HireDate;
            existing.IsActive     = employee.IsActive;
            existing.DepartmentId = employee.DepartmentId;

            await _context.SaveChangesAsync();

            // Reload with Department for the response body
            await _context.Entry(existing)
                .Reference(e => e.Department)
                .LoadAsync();

            return existing;
        }

        // -----------------------------------------------------------------------
        // DELETE
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return false;

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return true;
        }

        // -----------------------------------------------------------------------
        // EXISTS CHECK  — used by controller before issuing 404
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<bool> EmployeeExistsAsync(int id)
        {
            return await _context.Employees
                .AnyAsync(e => e.EmployeeId == id);
        }
    }
}
