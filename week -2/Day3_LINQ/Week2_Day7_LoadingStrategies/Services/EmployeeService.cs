using Microsoft.EntityFrameworkCore;
using Week2_Day7_LoadingStrategies.Data;
using Week2_Day7_LoadingStrategies.Models;

namespace Week2_Day7_LoadingStrategies.Services
{
    /// <summary>
    /// EmployeeService — Day 7 complete version.
    ///
    /// Carries forward all previous days:
    ///   Day 2/4 : CRUD  (AddEmployee, GetAll, GetById, Update, Delete)
    ///   Day 5   : LINQ  (GetByDept, SalaryFilter, GroupBy, Search, Sort...)
    ///   Day 7   : Loading Strategies + Performance Optimization
    ///             - Eager Loading   via Include()
    ///             - Explicit Loading via Entry().Reference/Collection
    ///             - Lazy Loading    via virtual nav properties (in AppDbContext)
    ///             - AsNoTracking()  for read-only queries
    ///             - Projections     via Select()
    /// </summary>
    public class EmployeeService
    {
        private readonly AppDbContext _context;

        public EmployeeService(AppDbContext context)
        {
            _context = context;
        }

        // =============================================================
        //  FROM DAY 2/4 — CRUD OPERATIONS (unchanged)
        // =============================================================

        public void AddEmployee(Employee employee)
        {
            _context.Employees.Add(employee);
            _context.SaveChanges();
            Console.WriteLine($"  [CREATE] Added: [{employee.EmployeeId}] {employee.Name}");
        }

        public List<Employee> GetAllEmployees()
        {
            return _context.Employees.OrderBy(e => e.EmployeeId).ToList();
        }

        public Employee? GetEmployeeById(int id)
        {
            return _context.Employees.FirstOrDefault(e => e.EmployeeId == id);
        }

        public void UpdateEmployee(int id, string name, string dept, decimal salary)
        {
            var emp = _context.Employees.Find(id);
            if (emp == null) { Console.WriteLine($"  [NOT FOUND] ID={id}"); return; }
            emp.Name = name; emp.Department = dept; emp.Salary = salary;
            _context.SaveChanges();
            Console.WriteLine($"  [UPDATE] ID={id} updated.");
        }

        public void DeleteEmployee(int id)
        {
            var emp = _context.Employees.Find(id);
            if (emp == null) { Console.WriteLine($"  [NOT FOUND] ID={id}"); return; }
            _context.Employees.Remove(emp);
            _context.SaveChanges();
            Console.WriteLine($"  [DELETE] Removed ID={id} ({emp.Name})");
        }

        // =============================================================
        //  FROM DAY 5 — LINQ QUERIES (unchanged)
        // =============================================================

        public List<Employee> GetEmployeesByDepartment(string department)
            => _context.Employees.Where(e => e.Department == department).ToList();

        public List<Employee> GetEmployeesWithSalaryGreaterThan(decimal salary)
            => _context.Employees.Where(e => e.Salary > salary)
                                 .OrderByDescending(e => e.Salary).ToList();

        public Employee? GetHighestPaidEmployee()
            => _context.Employees.OrderByDescending(e => e.Salary).FirstOrDefault();

        public decimal GetAverageSalary()
            => _context.Employees.Any() ? _context.Employees.Average(e => e.Salary) : 0;

        public int GetEmployeeCount()
            => _context.Employees.Count();

        public List<Employee> SearchEmployeeByName(string name)
            => _context.Employees.Where(e => e.Name.Contains(name))
                                 .OrderBy(e => e.Name).ToList();

        public List<Employee> SortEmployeesBySalary(bool ascending = true)
            => ascending ? _context.Employees.OrderBy(e => e.Salary).ToList()
                         : _context.Employees.OrderByDescending(e => e.Salary).ToList();

        public List<DepartmentSummary> GroupEmployeesByDepartment()
            => _context.Employees
                       .GroupBy(e => e.Department)
                       .Select(g => new DepartmentSummary
                       {
                           Department    = g.Key,
                           EmployeeCount = g.Count(),
                           AverageSalary = g.Average(e => e.Salary),
                           TotalSalary   = g.Sum(e => e.Salary),
                           HighestSalary = g.Max(e => e.Salary),
                           LowestSalary  = g.Min(e => e.Salary)
                       })
                       .OrderBy(g => g.Department).ToList();

        // =============================================================
        //  DAY 7 — LOADING STRATEGIES
        // =============================================================

        // ─────────────────────────────────────────────────────────────
        // STRATEGY 1: EAGER LOADING using Include()
        //
        // WHAT  : Loads the Employee AND its related Department in a
        //         single SQL JOIN query.
        // WHEN  : Use when you KNOW you need the related data upfront.
        //         Best for displaying combined data (e.g., employee list
        //         with department name and location).
        // SQL   : SELECT e.*, d.*
        //         FROM Employees e
        //         INNER JOIN Departments d ON e.DepartmentId = d.Id
        // PRO   : Single round trip to DB — efficient.
        // CON   : Loads data you may not need if related entity is large.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// EAGER LOADING: Loads all employees with their Department
        /// in a single JOIN query using Include().
        /// </summary>
        public List<Employee> GetAllEmployeesWithDept_EagerLoading()
        {
            return _context.Employees
                           .Include(e => e.Dept)          // JOIN Departments
                           .OrderBy(e => e.EmployeeId)
                           .ToList();
        }

        /// <summary>
        /// EAGER LOADING: Load a single employee with department by ID.
        /// </summary>
        public Employee? GetEmployeeWithDeptById_EagerLoading(int id)
        {
            return _context.Employees
                           .Include(e => e.Dept)
                           .FirstOrDefault(e => e.EmployeeId == id);
        }

        /// <summary>
        /// EAGER LOADING: Load departments with all their employees (reverse).
        /// ThenInclude() for nested loading.
        /// </summary>
        public List<Department> GetAllDepartmentsWithEmployees_EagerLoading()
        {
            return _context.Departments
                           .Include(d => d.Employees)     // load employees per dept
                           .OrderBy(d => d.Name)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // STRATEGY 2: EXPLICIT LOADING using Entry()
        //
        // WHAT  : First load the parent entity, THEN explicitly trigger
        //         a second query to load the related data on demand.
        // WHEN  : Use when you sometimes need the related data and
        //         sometimes don't — decide at runtime based on logic.
        //         Good for conditional loading without Lazy Loading overhead.
        // SQL   : Query 1: SELECT * FROM Employees WHERE EmployeeId = @id
        //         Query 2: SELECT * FROM Departments WHERE Id = @deptId
        // PRO   : Precise control — load only what you need, when you need it.
        // CON   : Two round trips to the database.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// EXPLICIT LOADING: Load employee first, then explicitly load
        /// its Department using Entry().Reference().Load().
        /// </summary>
        public Employee? GetEmployeeWithExplicitLoading(int id)
        {
            // Step 1: Load the employee (no JOIN, no related data)
            var employee = _context.Employees
                                   .FirstOrDefault(e => e.EmployeeId == id);

            if (employee != null)
            {
                // Step 2: Explicitly load the related Department
                // Entry() gives access to EF Core's change tracker for this entity.
                // Reference() is for single navigation properties (many-to-one).
                // Collection() is for collection navigation properties (one-to-many).
                _context.Entry(employee)
                        .Reference(e => e.Dept)   // single nav property
                        .Load();                   // fires second SQL query
            }

            return employee;
        }

        /// <summary>
        /// EXPLICIT LOADING: Load a Department, then explicitly load
        /// its Employees collection using Entry().Collection().Load().
        /// </summary>
        public Department? GetDepartmentWithExplicitEmployees(int deptId)
        {
            // Step 1: Load the department only
            var dept = _context.Departments
                               .FirstOrDefault(d => d.Id == deptId);

            if (dept != null)
            {
                // Step 2: Explicitly load the Employees collection
                // Collection() is for ICollection / List navigation properties.
                _context.Entry(dept)
                        .Collection(d => d.Employees)  // collection nav property
                        .Load();                         // fires second SQL query
            }

            return dept;
        }

        // ─────────────────────────────────────────────────────────────
        // STRATEGY 3: LAZY LOADING via virtual navigation properties
        //
        // WHAT  : Related data is loaded AUTOMATICALLY the first time
        //         the navigation property is accessed in code.
        //         EF Core Proxies intercept the property getter and
        //         issue a new SQL query behind the scenes.
        // WHEN  : Use in scenarios where you can't predict at query time
        //         whether related data will be needed.
        // SETUP : UseLazyLoadingProxies() in OnConfiguring +
        //         navigation properties must be virtual.
        // SQL   : Fired automatically when e.Dept is first read.
        // PRO   : Convenient — no need to think about loading upfront.
        // CON   : Risk of N+1 problem (1 query per entity in a loop).
        //         DbContext must still be open when property is accessed.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// LAZY LOADING: Returns employees WITHOUT Include().
        /// When Program.cs accesses e.Dept, EF Core Proxy automatically
        /// fires a SELECT to load the department for that employee.
        /// Note: This demonstrates N+1 — one extra query per employee.
        /// </summary>
        public List<Employee> GetAllEmployees_LazyLoading()
        {
            // No Include() — Dept will be loaded lazily on first access
            return _context.Employees
                           .OrderBy(e => e.EmployeeId)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // STRATEGY 4: AsNoTracking() for read-only queries
        //
        // WHAT  : Tells EF Core NOT to add loaded entities to the
        //         change tracker. The context won't watch for changes.
        // WHEN  : Use for read-only scenarios (reports, display, APIs)
        //         where you will NOT update the loaded entities.
        // SQL   : Same SQL as normal queries — the difference is only
        //         in EF Core's in-memory behavior.
        // PRO   : Faster and uses less memory — no change tracking overhead.
        //         Can be 20–30% faster on large result sets.
        // CON   : Cannot call SaveChanges() on these entities — EF Core
        //         won't detect changes since it's not tracking them.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// AsNoTracking: Read-only query — EF Core skips change tracking.
        /// Use for display/reporting where you don't need to update.
        /// </summary>
        public List<Employee> GetAllEmployees_AsNoTracking()
        {
            return _context.Employees
                           .AsNoTracking()               // skip change tracker
                           .Include(e => e.Dept)
                           .OrderBy(e => e.EmployeeId)
                           .ToList();
        }

        /// <summary>
        /// AsNoTracking with filter — read-only query for IT department.
        /// </summary>
        public List<Employee> GetEmployeesByDept_AsNoTracking(string dept)
        {
            return _context.Employees
                           .AsNoTracking()
                           .Where(e => e.Department == dept)
                           .Include(e => e.Dept)
                           .OrderBy(e => e.Name)
                           .ToList();
        }

        // =============================================================
        //  DAY 7 — PERFORMANCE OPTIMIZATION QUERIES
        // =============================================================

        // ─────────────────────────────────────────────────────────────
        // OPT 1: SELECT Projection — only fetch needed columns
        //
        // Instead of SELECT *, project only the columns you need.
        // Reduces data transfer between DB and application.
        // SQL: SELECT Name, Department, Salary FROM Employees
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// PROJECTION: Select only Name, Department, Salary + DeptLocation.
        /// Avoids fetching unnecessary columns (JoinedDate, FK, etc).
        /// </summary>
        public List<EmployeeProjection> GetEmployeeProjections()
        {
            return _context.Employees
                           .AsNoTracking()
                           .Include(e => e.Dept)
                           .Select(e => new EmployeeProjection
                           {
                               EmployeeId     = e.EmployeeId,
                               Name           = e.Name,
                               Department     = e.Department,
                               Salary         = e.Salary,
                               DeptLocation   = e.Dept != null ? e.Dept.Location : "N/A"
                           })
                           .OrderBy(e => e.EmployeeId)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // OPT 2: Where() filter pushed to DB — never load all then filter
        //
        // Always filter in the LINQ query so EF Core adds WHERE clause.
        // Never do: GetAll().Where(...) — that loads all rows first.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// PERFORMANCE: Filter pushed to SQL — only matching rows fetched.
        /// SQL: SELECT * FROM Employees WHERE Salary > @min AND Salary &lt; @max
        /// </summary>
        public List<Employee> GetEmployeesInSalaryRange(decimal min, decimal max)
        {
            return _context.Employees
                           .AsNoTracking()
                           .Where(e => e.Salary >= min && e.Salary <= max)
                           .Include(e => e.Dept)
                           .OrderBy(e => e.Salary)
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // OPT 3: OrderBy() + Take() — TOP N query (pagination)
        //
        // Fetch only the top N rows instead of all rows.
        // SQL: SELECT TOP 3 * FROM Employees ORDER BY Salary DESC
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// PERFORMANCE: Take() limits rows fetched from DB.
        /// Returns top N highest-paid employees.
        /// </summary>
        public List<Employee> GetTopNHighestPaid(int n)
        {
            return _context.Employees
                           .AsNoTracking()
                           .Include(e => e.Dept)
                           .OrderByDescending(e => e.Salary)
                           .Take(n)                         // TOP N in SQL
                           .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // OPT 4: FirstOrDefault() — stops after finding 1 row
        //
        // More efficient than ToList().First() which loads all rows.
        // SQL: SELECT TOP 1 * FROM Employees WHERE EmployeeId = @id
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// PERFORMANCE: FirstOrDefault with Include — single row + JOIN.
        /// </summary>
        public Employee? FindEmployeeWithDept(int id)
        {
            return _context.Employees
                           .AsNoTracking()
                           .Include(e => e.Dept)
                           .FirstOrDefault(e => e.EmployeeId == id);
        }

        // ─────────────────────────────────────────────────────────────
        // OPT 5: Combine Where + Select + OrderBy + Take (full pipeline)
        //
        // Best practice: push all operations to SQL, never to memory.
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// FULL OPTIMIZED PIPELINE: Filter → Project → Sort → Limit.
        /// All executed as a single SQL query.
        /// </summary>
        public List<EmployeeProjection> GetOptimizedTopEarnersByDept(string dept, int top)
        {
            return _context.Employees
                           .AsNoTracking()
                           .Where(e => e.Department == dept)          // filter
                           .Include(e => e.Dept)
                           .Select(e => new EmployeeProjection         // project
                           {
                               EmployeeId   = e.EmployeeId,
                               Name         = e.Name,
                               Department   = e.Department,
                               Salary       = e.Salary,
                               DeptLocation = e.Dept != null ? e.Dept.Location : "N/A"
                           })
                           .OrderByDescending(e => e.Salary)           // sort
                           .Take(top)                                   // limit
                           .ToList();
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // DTOs
    // ─────────────────────────────────────────────────────────────────

    public class DepartmentSummary
    {
        public string  Department    { get; set; } = string.Empty;
        public int     EmployeeCount { get; set; }
        public decimal AverageSalary { get; set; }
        public decimal TotalSalary   { get; set; }
        public decimal HighestSalary { get; set; }
        public decimal LowestSalary  { get; set; }
    }

    public class EmployeeProjection
    {
        public int     EmployeeId   { get; set; }
        public string  Name         { get; set; } = string.Empty;
        public string  Department   { get; set; } = string.Empty;
        public decimal Salary       { get; set; }
        public string  DeptLocation { get; set; } = string.Empty;
    }
}
