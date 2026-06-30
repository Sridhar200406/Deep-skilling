using Microsoft.EntityFrameworkCore;
using Week2_Day5_EFCoreLinq.Data;
using Week2_Day5_EFCoreLinq.Models;

namespace Week2_Day5_EFCoreLinq.Services
{
    /// <summary>
    /// EmployeeService encapsulates all database operations for Employee.
    /// - CRUD methods carried over from Day 2
    /// - LINQ query methods added for Day 5
    /// </summary>
    public class EmployeeService
    {
        private readonly AppDbContext _context;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        // =============================================================
        //  EXISTING CRUD METHODS (from Day 2 — DO NOT MODIFY)
        // =============================================================

        /// <summary>CREATE — Add a new employee to the database.</summary>
        public void AddEmployee(Employee employee)
        {
            _context.Employees.Add(employee);
            _context.SaveChanges();
            Console.WriteLine($"  [CREATE] Added: [{employee.EmployeeId}] {employee.Name}");
        }

        /// <summary>READ — Get a single employee by ID.</summary>
        public Employee? GetEmployeeById(int id)
        {
            return _context.Employees.FirstOrDefault(e => e.EmployeeId == id);
        }

        /// <summary>UPDATE — Update an existing employee's details.</summary>
        public void UpdateEmployee(Employee employee)
        {
            _context.Employees.Update(employee);
            _context.SaveChanges();
            Console.WriteLine($"  [UPDATE] Updated: [{employee.EmployeeId}] {employee.Name}");
        }

        /// <summary>DELETE — Remove an employee by ID.</summary>
        public void DeleteEmployee(int id)
        {
            var emp = _context.Employees.Find(id);
            if (emp != null)
            {
                _context.Employees.Remove(emp);
                _context.SaveChanges();
                Console.WriteLine($"  [DELETE] Removed: [{id}] {emp.Name}");
            }
        }

        // =============================================================
        //  DAY 5 — LINQ QUERY METHODS
        // =============================================================

        // ─────────────────────────────────────────────────────────────
        // METHOD 1: GetAllEmployees()
        // LINQ: Retrieves every row from the Employees table using
        //       ToList() which translates to SELECT * FROM Employees.
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns all employees from the database.
        /// LINQ: employees.ToList()
        /// SQL : SELECT * FROM Employees
        /// </summary>
        public List<Employee> GetAllEmployees()
        {
            return _context.Employees.ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 2: GetEmployeesByDepartment(string department)
        // LINQ: Uses Where() to filter employees whose Department
        //       matches the given string (case-insensitive via EF Core).
        // SQL : SELECT * FROM Employees WHERE Department = @dept
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns all employees in the specified department.
        /// LINQ: employees.Where(e => e.Department == department).ToList()
        /// </summary>
        public List<Employee> GetEmployeesByDepartment(string department)
        {
            return _context.Employees
                           .Where(e => e.Department == department)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 3: GetEmployeesWithSalaryGreaterThan(decimal salary)
        // LINQ: Uses Where() with a comparison operator to filter
        //       employees whose Salary exceeds the given threshold.
        // SQL : SELECT * FROM Employees WHERE Salary > @salary
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns employees whose salary is greater than the given amount.
        /// LINQ: employees.Where(e => e.Salary > salary).ToList()
        /// </summary>
        public List<Employee> GetEmployeesWithSalaryGreaterThan(decimal salary)
        {
            return _context.Employees
                           .Where(e => e.Salary > salary)
                           .OrderByDescending(e => e.Salary)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 4: GetHighestPaidEmployee()
        // LINQ: Sorts all employees by Salary descending and takes
        //       the first result — i.e., the max salary employee.
        // SQL : SELECT TOP 1 * FROM Employees ORDER BY Salary DESC
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns the employee with the highest salary.
        /// LINQ: employees.OrderByDescending(e => e.Salary).FirstOrDefault()
        /// </summary>
        public Employee? GetHighestPaidEmployee()
        {
            return _context.Employees
                           .OrderByDescending(e => e.Salary)
                           .FirstOrDefault();
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 5: GetAverageSalary()
        // LINQ: Uses Average() aggregate to compute the mean salary
        //       across all employees.
        // SQL : SELECT AVG(Salary) FROM Employees
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns the average salary of all employees.
        /// LINQ: employees.Average(e => e.Salary)
        /// </summary>
        public decimal GetAverageSalary()
        {
            if (!_context.Employees.Any()) return 0;
            return _context.Employees.Average(e => e.Salary);
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 6: GetEmployeeCount()
        // LINQ: Uses Count() to return the total number of rows.
        // SQL : SELECT COUNT(*) FROM Employees
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns the total number of employees in the database.
        /// LINQ: employees.Count()
        /// </summary>
        public int GetEmployeeCount()
        {
            return _context.Employees.Count();
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 7: SearchEmployeeByName(string name)
        // LINQ: Uses Where() + Contains() for a partial name search.
        //       EF Core translates Contains() to SQL LIKE '%name%'.
        // SQL : SELECT * FROM Employees WHERE Name LIKE '%@name%'
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Searches employees by partial name match (case-insensitive).
        /// LINQ: employees.Where(e => e.Name.Contains(name)).ToList()
        /// </summary>
        public List<Employee> SearchEmployeeByName(string name)
        {
            return _context.Employees
                           .Where(e => e.Name.Contains(name))
                           .OrderBy(e => e.Name)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 8: SortEmployeesBySalary()
        // LINQ: Uses OrderBy() and OrderByDescending() to return
        //       employees sorted by salary in both directions.
        // SQL : SELECT * FROM Employees ORDER BY Salary ASC/DESC
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Returns all employees sorted by salary.
        /// ascending = true  → lowest to highest
        /// ascending = false → highest to lowest
        /// LINQ: employees.OrderBy(e => e.Salary) or
        ///       employees.OrderByDescending(e => e.Salary)
        /// </summary>
        public List<Employee> SortEmployeesBySalary(bool ascending = true)
        {
            return ascending
                ? _context.Employees.OrderBy(e => e.Salary).ToList()
                : _context.Employees.OrderByDescending(e => e.Salary).ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // METHOD 9: GroupEmployeesByDepartment()
        // LINQ: Uses GroupBy() to group employees by their Department,
        //       then projects each group into a summary object with
        //       department name, employee count, and average salary.
        // SQL : SELECT Department, COUNT(*), AVG(Salary)
        //       FROM Employees GROUP BY Department
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Groups employees by department and returns a summary
        /// with count and average salary per department.
        /// LINQ: employees.GroupBy(e => e.Department)
        ///                .Select(g => new { Dept, Count, AvgSalary })
        /// </summary>
        public List<DepartmentSummary> GroupEmployeesByDepartment()
        {
            return _context.Employees
                           .GroupBy(e => e.Department)
                           .Select(g => new DepartmentSummary
                           {
                               Department = g.Key,
                               EmployeeCount = g.Count(),
                               AverageSalary = g.Average(e => e.Salary),
                               TotalSalary   = g.Sum(e => e.Salary),
                               HighestSalary = g.Max(e => e.Salary),
                               LowestSalary  = g.Min(e => e.Salary)
                           })
                           .OrderBy(g => g.Department)
                           .ToList();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Helper DTO for GroupEmployeesByDepartment result
    // ─────────────────────────────────────────────────────────────────
    public class DepartmentSummary
    {
        public string Department   { get; set; } = string.Empty;
        public int    EmployeeCount { get; set; }
        public decimal AverageSalary { get; set; }
        public decimal TotalSalary   { get; set; }
        public decimal HighestSalary { get; set; }
        public decimal LowestSalary  { get; set; }
    }
}
