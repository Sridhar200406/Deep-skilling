using Week2_Day4_EFCoreCRUD.Data;
using Week2_Day4_EFCoreCRUD.Models;

namespace Week2_Day4_EFCoreCRUD.Services
{
    /// <summary>
    /// EmployeeService — encapsulates all CRUD operations for the Employee entity.
    /// Uses AppDbContext to interact with the SQL Server database via EF Core.
    ///
    /// Methods:
    ///   1. AddEmployee(Employee)          — CREATE
    ///   2. GetAllEmployees()              — READ all
    ///   3. GetEmployeeById(int id)        — READ single
    ///   4. UpdateEmployee(int, Employee)  — UPDATE
    ///   5. DeleteEmployee(int id)         — DELETE
    /// </summary>
    public class EmployeeService
    {
        // Injected DbContext — single instance shared across all methods
        private readonly AppDbContext _context;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // METHOD 1: AddEmployee  (CREATE)
        // -----------------------------------------------------------------
        // What it does:
        //   Validates the employee object, then adds it to the Employees
        //   DbSet and calls SaveChanges() to persist it to the database.
        //
        // EF Core: _context.Employees.Add(employee)  →  INSERT INTO Employees
        // SQL    : INSERT INTO Employees (Name, Department, Salary)
        //          VALUES (@Name, @Department, @Salary)
        // =================================================================
        /// <summary>
        /// Adds a new employee record to the database.
        /// </summary>
        /// <param name="employee">Employee object to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if employee is null.</exception>
        /// <exception cref="ArgumentException">Thrown if Name or Department is empty, or Salary is negative.</exception>
        public void AddEmployee(Employee employee)
        {
            // Validation
            if (employee == null)
                throw new ArgumentNullException(nameof(employee), "Employee cannot be null.");

            if (string.IsNullOrWhiteSpace(employee.Name))
                throw new ArgumentException("Employee name cannot be empty.", nameof(employee.Name));

            if (string.IsNullOrWhiteSpace(employee.Department))
                throw new ArgumentException("Department cannot be empty.", nameof(employee.Department));

            if (employee.Salary < 0)
                throw new ArgumentException("Salary cannot be negative.", nameof(employee.Salary));

            try
            {
                _context.Employees.Add(employee);
                _context.SaveChanges();
                Console.WriteLine($"  [SUCCESS] Employee added: ID={employee.EmployeeId}, Name={employee.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Failed to add employee: {ex.Message}");
                throw;
            }
        }

        // =================================================================
        // METHOD 2: GetAllEmployees  (READ — all records)
        // -----------------------------------------------------------------
        // What it does:
        //   Fetches every row from the Employees table and returns them
        //   as a List<Employee> ordered by EmployeeId ascending.
        //
        // EF Core: _context.Employees.OrderBy(...).ToList()
        // SQL    : SELECT * FROM Employees ORDER BY EmployeeId ASC
        // =================================================================
        /// <summary>
        /// Returns all employees from the database, ordered by ID.
        /// </summary>
        /// <returns>List of all Employee records.</returns>
        public List<Employee> GetAllEmployees()
        {
            try
            {
                return _context.Employees
                               .OrderBy(e => e.EmployeeId)
                               .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Failed to retrieve employees: {ex.Message}");
                throw;
            }
        }

        // =================================================================
        // METHOD 3: GetEmployeeById  (READ — single record)
        // -----------------------------------------------------------------
        // What it does:
        //   Searches the Employees table for a row where EmployeeId equals
        //   the given id. Returns null if no match is found.
        //
        // EF Core: _context.Employees.FirstOrDefault(e => e.EmployeeId == id)
        // SQL    : SELECT TOP 1 * FROM Employees WHERE EmployeeId = @id
        // =================================================================
        /// <summary>
        /// Returns a single employee by their ID, or null if not found.
        /// </summary>
        /// <param name="id">The EmployeeId to search for.</param>
        /// <returns>Employee object or null.</returns>
        /// <exception cref="ArgumentException">Thrown if id is less than 1.</exception>
        public Employee? GetEmployeeById(int id)
        {
            if (id < 1)
                throw new ArgumentException("Employee ID must be greater than 0.", nameof(id));

            try
            {
                var employee = _context.Employees
                                       .FirstOrDefault(e => e.EmployeeId == id);

                if (employee == null)
                    Console.WriteLine($"  [NOT FOUND] No employee found with ID={id}");

                return employee;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Failed to find employee: {ex.Message}");
                throw;
            }
        }

        // =================================================================
        // METHOD 4: UpdateEmployee  (UPDATE)
        // -----------------------------------------------------------------
        // What it does:
        //   Finds the existing employee by id, then applies only the fields
        //   that are provided in updatedEmployee (Name, Department, Salary).
        //   Calls SaveChanges() to commit the changes to the database.
        //
        // EF Core: Tracks the entity change automatically, SaveChanges()
        //          generates the UPDATE SQL.
        // SQL    : UPDATE Employees
        //          SET Name=@Name, Department=@Dept, Salary=@Salary
        //          WHERE EmployeeId = @id
        // =================================================================
        /// <summary>
        /// Updates an existing employee's Name, Department, and/or Salary.
        /// Only non-null / non-empty values from updatedEmployee are applied.
        /// </summary>
        /// <param name="id">ID of the employee to update.</param>
        /// <param name="updatedEmployee">Object containing updated field values.</param>
        /// <exception cref="ArgumentException">Thrown if id &lt; 1 or updatedEmployee is null.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if no employee exists with the given id.</exception>
        public void UpdateEmployee(int id, Employee updatedEmployee)
        {
            if (id < 1)
                throw new ArgumentException("Employee ID must be greater than 0.", nameof(id));

            if (updatedEmployee == null)
                throw new ArgumentNullException(nameof(updatedEmployee), "Updated employee data cannot be null.");

            if (updatedEmployee.Salary < 0)
                throw new ArgumentException("Salary cannot be negative.", nameof(updatedEmployee.Salary));

            try
            {
                // Find the existing record in the database
                var existing = _context.Employees.FirstOrDefault(e => e.EmployeeId == id);

                if (existing == null)
                    throw new KeyNotFoundException($"Employee with ID={id} not found.");

                // Track what changed (for console output)
                string changes = "";

                // Apply Name update if provided
                if (!string.IsNullOrWhiteSpace(updatedEmployee.Name) &&
                    updatedEmployee.Name != existing.Name)
                {
                    changes += $" Name: '{existing.Name}'->'{updatedEmployee.Name}'";
                    existing.Name = updatedEmployee.Name;
                }

                // Apply Department update if provided
                if (!string.IsNullOrWhiteSpace(updatedEmployee.Department) &&
                    updatedEmployee.Department != existing.Department)
                {
                    changes += $" Dept: '{existing.Department}'->'{updatedEmployee.Department}'";
                    existing.Department = updatedEmployee.Department;
                }

                // Apply Salary update if provided (0 means no change)
                if (updatedEmployee.Salary > 0 &&
                    updatedEmployee.Salary != existing.Salary)
                {
                    changes += $" Salary: {existing.Salary}->{updatedEmployee.Salary}";
                    existing.Salary = updatedEmployee.Salary;
                }

                _context.SaveChanges();
                Console.WriteLine($"  [SUCCESS] Employee ID={id} updated.{changes}");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Failed to update employee: {ex.Message}");
                throw;
            }
        }

        // =================================================================
        // METHOD 5: DeleteEmployee  (DELETE)
        // -----------------------------------------------------------------
        // What it does:
        //   Finds the employee by id, removes it from the DbSet, and calls
        //   SaveChanges() to execute the DELETE SQL statement.
        //
        // EF Core: _context.Employees.Remove(employee)
        // SQL    : DELETE FROM Employees WHERE EmployeeId = @id
        // =================================================================
        /// <summary>
        /// Deletes an employee from the database by their ID.
        /// </summary>
        /// <param name="id">The EmployeeId to delete.</param>
        /// <exception cref="ArgumentException">Thrown if id &lt; 1.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if no employee with the given id exists.</exception>
        public void DeleteEmployee(int id)
        {
            if (id < 1)
                throw new ArgumentException("Employee ID must be greater than 0.", nameof(id));

            try
            {
                var employee = _context.Employees.FirstOrDefault(e => e.EmployeeId == id);

                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID={id} not found.");

                _context.Employees.Remove(employee);
                _context.SaveChanges();
                Console.WriteLine($"  [SUCCESS] Deleted: ID={id}, Name={employee.Name}");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] Failed to delete employee: {ex.Message}");
                throw;
            }
        }

        // =================================================================
        // HELPER: PrintEmployees
        // Prints a formatted table of employees to the console.
        // =================================================================
        /// <summary>
        /// Prints all employees in a formatted table.
        /// </summary>
        public void PrintEmployees(List<Employee> employees)
        {
            if (!employees.Any())
            {
                Console.WriteLine("  (no records found)");
                return;
            }

            Console.WriteLine($"  {"ID",-6} {"Name",-12} {"Department",-12} {"Salary",10}");
            Console.WriteLine($"  {"------",-6} {"------------",-12} {"------------",-12} {"----------",10}");
            foreach (var e in employees)
                Console.WriteLine($"  {e.EmployeeId,-6} {e.Name,-12} {e.Department,-12} {e.Salary,10:C0}");
            Console.WriteLine($"  Total: {employees.Count} record(s)");
        }
    }
}
