using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Repositories
{
    /// <summary>
    /// Concrete implementation of IEmployeeRepository — Day 5 update.
    /// Added ILogger logging at Information and Error levels.
    /// Exceptions propagate up — ExceptionMiddleware handles them globally.
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(AppDbContext context, ILogger<EmployeeRepository> logger)
        {
            _context = context;
            _logger  = logger;
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            _logger.LogInformation("EmployeeRepository: Executing GetAllAsync — SELECT all employees with Department JOIN.");

            var employees = await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("EmployeeRepository: GetAllAsync returned {Count} records.", employees.Count);
            return employees;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            _logger.LogInformation("EmployeeRepository: Executing GetByIdAsync for ID {Id}.", id);

            var employee = await _context.Employees
                .Include(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                _logger.LogWarning("EmployeeRepository: No record found for ID {Id}.", id);
            else
                _logger.LogInformation("EmployeeRepository: Found employee ID {Id}.", id);

            return employee;
        }

        public async Task AddAsync(Employee employee)
        {
            _logger.LogInformation("EmployeeRepository: Executing AddAsync — INSERT new employee {FirstName} {LastName}.",
                employee.FirstName, employee.LastName);

            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("EmployeeRepository: INSERT complete — new EmployeeId = {Id}.", employee.EmployeeId);
        }

        public async Task UpdateAsync(Employee employee)
        {
            _logger.LogInformation("EmployeeRepository: Executing UpdateAsync — UPDATE employee ID {Id}.", employee.EmployeeId);

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("EmployeeRepository: UPDATE complete for ID {Id}.", employee.EmployeeId);
        }

        public async Task DeleteAsync(Employee employee)
        {
            _logger.LogInformation("EmployeeRepository: Executing DeleteAsync — DELETE employee ID {Id}.", employee.EmployeeId);

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("EmployeeRepository: DELETE complete for ID {Id}.", employee.EmployeeId);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            _logger.LogInformation("EmployeeRepository: Executing ExistsAsync for ID {Id}.", id);

            return await _context.Employees.AnyAsync(e => e.EmployeeId == id);
        }

        public async Task LoadDepartmentAsync(Employee employee)
        {
            _logger.LogInformation("EmployeeRepository: Loading Department navigation for employee ID {Id}.", employee.EmployeeId);

            await _context.Entry(employee)
                .Reference(e => e.Department)
                .LoadAsync();
        }
    }
}
