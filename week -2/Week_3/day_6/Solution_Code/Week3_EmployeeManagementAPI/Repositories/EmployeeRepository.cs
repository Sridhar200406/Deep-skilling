using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Data;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Repositories
{
    /// <summary>Concrete implementation of IEmployeeRepository — unchanged from Day 5.</summary>
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
            _logger.LogInformation("EmployeeRepository: GetAllAsync.");
            var employees = await _context.Employees.Include(e => e.Department).AsNoTracking().ToListAsync();
            _logger.LogInformation("EmployeeRepository: Returned {Count} records.", employees.Count);
            return employees;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            _logger.LogInformation("EmployeeRepository: GetByIdAsync ID {Id}.", id);
            var employee = await _context.Employees.Include(e => e.Department).AsNoTracking()
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

        public async Task LoadDepartmentAsync(Employee employee)
        {
            await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        }
    }
}
