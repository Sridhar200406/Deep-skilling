using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Services
{
    /// <summary>
    /// Concrete implementation of IEmployeeService.
    /// Day 3 refactor: EmployeeService no longer touches AppDbContext directly.
    /// All data access is delegated to IEmployeeRepository.
    /// 
    /// Dependency chain:
    ///   Controller → IEmployeeService → IEmployeeRepository → AppDbContext → SQL Server
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;

        // IEmployeeRepository is injected by the DI container
        public EmployeeService(IEmployeeRepository repository)
        {
            _repository = repository;
        }

        // -----------------------------------------------------------------------
        // GET ALL
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _repository.GetAllAsync();
        }

        // -----------------------------------------------------------------------
        // GET BY ID
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // -----------------------------------------------------------------------
        // CREATE
        // Business rule: client must not dictate the primary key — force it to 0
        // so the DB IDENTITY column always assigns the real ID.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            employee.EmployeeId = 0;                    // force DB to assign ID
            await _repository.AddAsync(employee);       // INSERT + SaveChanges
            await _repository.LoadDepartmentAsync(employee); // reload nav property
            return employee;
        }

        // -----------------------------------------------------------------------
        // UPDATE
        // Load the existing tracked entity first, apply field-by-field changes,
        // then save. This prevents overwriting columns the client didn't send.
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<Employee?> UpdateEmployeeAsync(int id, Employee employee)
        {
            // GetByIdAsync uses AsNoTracking — we need a tracked entity for Update
            // so we use FindTrackedAsync via the repository's GetByIdAsync workaround:
            // We reload via GetByIdAsync (read) then attach for update.
            var existing = await _repository.GetByIdAsync(id);

            if (existing == null)
                return null;

            // Apply updatable fields — EmployeeId is never changed
            existing.FirstName    = employee.FirstName;
            existing.LastName     = employee.LastName;
            existing.Email        = employee.Email;
            existing.Phone        = employee.Phone;
            existing.Position     = employee.Position;
            existing.Salary       = employee.Salary;
            existing.HireDate     = employee.HireDate;
            existing.IsActive     = employee.IsActive;
            existing.DepartmentId = employee.DepartmentId;

            await _repository.UpdateAsync(existing);        // UPDATE + SaveChanges
            await _repository.LoadDepartmentAsync(existing); // reload nav property
            return existing;
        }

        // -----------------------------------------------------------------------
        // DELETE
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _repository.GetByIdAsync(id);

            if (employee == null)
                return false;

            await _repository.DeleteAsync(employee);    // DELETE + SaveChanges
            return true;
        }

        // -----------------------------------------------------------------------
        // EXISTS CHECK
        // -----------------------------------------------------------------------
        /// <inheritdoc />
        public async Task<bool> EmployeeExistsAsync(int id)
        {
            return await _repository.ExistsAsync(id);
        }
    }
}
