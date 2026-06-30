using Week2_Day5_EFCoreLinq.Data;
using Week2_Day5_EFCoreLinq.Models;
using Week2_Day5_EFCoreLinq.Services;

// ── Bootstrap ────────────────────────────────────────────────
using var context = new AppDbContext();
context.Database.EnsureCreated();          // Creates DB/table if not exists
var service = new EmployeeService(context);

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  Week 2 - Day 5: EF Core + LINQ Queries          ║");
Console.WriteLine("║  Employee Management System                      ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");

// =============================================================
//  SEED DATA (only if table is empty)
// =============================================================
if (!context.Employees.Any())
{
    Console.WriteLine("\n[SEED] Inserting sample employees...");
    var seedData = new List<Employee>
    {
        new Employee { Name = "John",   Department = "IT",      Salary = 50000 },
        new Employee { Name = "David",  Department = "HR",      Salary = 35000 },
        new Employee { Name = "Mary",   Department = "IT",      Salary = 60000 },
        new Employee { Name = "Smith",  Department = "Finance", Salary = 45000 },
        new Employee { Name = "James",  Department = "HR",      Salary = 40000 },
        new Employee { Name = "Alice",  Department = "IT",      Salary = 55000 },
        new Employee { Name = "Robert", Department = "Finance", Salary = 48000 },
        new Employee { Name = "Jane",   Department = "HR",      Salary = 38000 },
    };
    seedData.ForEach(service.AddEmployee);
}

// =============================================================
//  METHOD 1: GetAllEmployees()
//  LINQ  : _context.Employees.ToList()
//  What  : Fetches every row from Employees table
//  SQL   : SELECT * FROM Employees
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 1: GetAllEmployees()                     ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : _context.Employees.ToList()");
Console.WriteLine("  SQL  : SELECT * FROM Employees\n");

var allEmployees = service.GetAllEmployees();
Console.WriteLine($"  {"ID",-5} {"Name",-10} {"Department",-12} {"Salary"}");
Console.WriteLine($"  {"--",-5} {"----",-10} {"----------",-12} {"------"}");
foreach (var e in allEmployees)
    Console.WriteLine($"  {e.EmployeeId,-5} {e.Name,-10} {e.Department,-12} {e.Salary:C0}");

// Expected Output:
//   ID    Name       Department   Salary
//   1     John       IT           $50,000
//   2     David      HR           $35,000
//   3     Mary       IT           $60,000  ... etc.

// =============================================================
//  METHOD 2: GetEmployeesByDepartment(string department)
//  LINQ  : .Where(e => e.Department == department).ToList()
//  What  : Filters rows matching the given department
//  SQL   : SELECT * FROM Employees WHERE Department = 'IT'
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 2: GetEmployeesByDepartment(\"IT\")        ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : .Where(e => e.Department == \"IT\").ToList()");
Console.WriteLine("  SQL  : SELECT * FROM Employees WHERE Department = 'IT'\n");

var itEmployees = service.GetEmployeesByDepartment("IT");
Console.WriteLine($"  Found {itEmployees.Count} employee(s) in IT:");
foreach (var e in itEmployees)
    Console.WriteLine($"  [{e.EmployeeId}] {e.Name,-10} | {e.Department,-10} | {e.Salary:C0}");

// Expected Output:
//   Found 3 employee(s) in IT:
//   [1] John       | IT         | $50,000
//   [3] Mary       | IT         | $60,000
//   [6] Alice      | IT         | $55,000

// =============================================================
//  METHOD 3: GetEmployeesWithSalaryGreaterThan(decimal salary)
//  LINQ  : .Where(e => e.Salary > salary).OrderByDescending(...)
//  What  : Filters employees earning more than the threshold
//  SQL   : SELECT * FROM Employees WHERE Salary > 45000
//          ORDER BY Salary DESC
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 3: GetEmployeesWithSalaryGreaterThan(45000)║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : .Where(e => e.Salary > 45000)");
Console.WriteLine("          .OrderByDescending(e => e.Salary).ToList()");
Console.WriteLine("  SQL  : SELECT * FROM Employees WHERE Salary > 45000 ORDER BY Salary DESC\n");

var highEarners = service.GetEmployeesWithSalaryGreaterThan(45000);
Console.WriteLine($"  Found {highEarners.Count} employee(s) with Salary > $45,000:");
foreach (var e in highEarners)
    Console.WriteLine($"  [{e.EmployeeId}] {e.Name,-10} | {e.Department,-10} | {e.Salary:C0}");

// Expected Output:
//   Found 4 employee(s) with Salary > $45,000:
//   [3] Mary       | IT         | $60,000
//   [6] Alice      | IT         | $55,000
//   [1] John       | IT         | $50,000
//   [7] Robert     | Finance    | $48,000

// =============================================================
//  METHOD 4: GetHighestPaidEmployee()
//  LINQ  : .OrderByDescending(e => e.Salary).FirstOrDefault()
//  What  : Sorts all employees by salary desc, takes the first
//  SQL   : SELECT TOP 1 * FROM Employees ORDER BY Salary DESC
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 4: GetHighestPaidEmployee()              ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : .OrderByDescending(e => e.Salary).FirstOrDefault()");
Console.WriteLine("  SQL  : SELECT TOP 1 * FROM Employees ORDER BY Salary DESC\n");

var topEarner = service.GetHighestPaidEmployee();
if (topEarner != null)
    Console.WriteLine($"  Highest Paid: [{topEarner.EmployeeId}] {topEarner.Name} | {topEarner.Department} | {topEarner.Salary:C0}");

// Expected Output:
//   Highest Paid: [3] Mary | IT | $60,000

// =============================================================
//  METHOD 5: GetAverageSalary()
//  LINQ  : .Average(e => e.Salary)
//  What  : Computes the mean salary across all employees
//  SQL   : SELECT AVG(Salary) FROM Employees
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 5: GetAverageSalary()                    ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : _context.Employees.Average(e => e.Salary)");
Console.WriteLine("  SQL  : SELECT AVG(Salary) FROM Employees\n");

var avgSalary = service.GetAverageSalary();
Console.WriteLine($"  Average Salary: {avgSalary:C0}");

// Expected Output:
//   Average Salary: $46,375

// =============================================================
//  METHOD 6: GetEmployeeCount()
//  LINQ  : .Count()
//  What  : Returns the total number of employees (rows)
//  SQL   : SELECT COUNT(*) FROM Employees
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 6: GetEmployeeCount()                    ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : _context.Employees.Count()");
Console.WriteLine("  SQL  : SELECT COUNT(*) FROM Employees\n");

var count = service.GetEmployeeCount();
Console.WriteLine($"  Total Employees: {count}");

// Expected Output:
//   Total Employees: 8

// =============================================================
//  METHOD 7: SearchEmployeeByName(string name)
//  LINQ  : .Where(e => e.Name.Contains(name)).ToList()
//  What  : Partial name search — EF Core translates Contains()
//          to SQL LIKE '%name%'
//  SQL   : SELECT * FROM Employees WHERE Name LIKE '%a%'
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 7: SearchEmployeeByName(\"a\")             ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : .Where(e => e.Name.Contains(\"a\")).OrderBy(e => e.Name).ToList()");
Console.WriteLine("  SQL  : SELECT * FROM Employees WHERE Name LIKE '%a%' ORDER BY Name\n");

var searchResults = service.SearchEmployeeByName("a");
Console.WriteLine($"  Found {searchResults.Count} employee(s) with 'a' in name:");
foreach (var e in searchResults)
    Console.WriteLine($"  [{e.EmployeeId}] {e.Name,-10} | {e.Department,-10} | {e.Salary:C0}");

// Expected Output:
//   Found 4 employee(s) with 'a' in name:
//   [6] Alice      | IT         | $55,000
//   [2] David      | HR         | $35,000
//   [5] James      | HR         | $40,000
//   [3] Mary       | IT         | $60,000

// =============================================================
//  METHOD 8: SortEmployeesBySalary()
//  LINQ  : .OrderBy(e => e.Salary) or .OrderByDescending(...)
//  What  : Sorts all employees by salary ascending or descending
//  SQL   : SELECT * FROM Employees ORDER BY Salary ASC/DESC
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 8: SortEmployeesBySalary()               ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");

Console.WriteLine("\n  [ASC] LINQ: .OrderBy(e => e.Salary).ToList()");
Console.WriteLine("        SQL : SELECT * FROM Employees ORDER BY Salary ASC\n");
var sortedAsc = service.SortEmployeesBySalary(ascending: true);
foreach (var e in sortedAsc)
    Console.WriteLine($"  {e.Name,-10} | {e.Department,-10} | {e.Salary:C0}");

Console.WriteLine("\n  [DESC] LINQ: .OrderByDescending(e => e.Salary).ToList()");
Console.WriteLine("         SQL : SELECT * FROM Employees ORDER BY Salary DESC\n");
var sortedDesc = service.SortEmployeesBySalary(ascending: false);
foreach (var e in sortedDesc)
    Console.WriteLine($"  {e.Name,-10} | {e.Department,-10} | {e.Salary:C0}");

// Expected Output (ASC):
//   David      | HR         | $35,000
//   Jane       | HR         | $38,000
//   James      | HR         | $40,000  ... up to Mary $60,000
//
// Expected Output (DESC):
//   Mary       | IT         | $60,000
//   Alice      | IT         | $55,000  ... down to David $35,000

// =============================================================
//  METHOD 9: GroupEmployeesByDepartment()
//  LINQ  : .GroupBy(e => e.Department)
//          .Select(g => new { Dept, Count, AvgSalary, ... })
//  What  : Groups all employees by department and computes
//          count, avg, total, min, max salary per group
//  SQL   : SELECT Department, COUNT(*), AVG(Salary), SUM(Salary),
//          MAX(Salary), MIN(Salary) FROM Employees GROUP BY Department
// =============================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  METHOD 9: GroupEmployeesByDepartment()          ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine("  LINQ : .GroupBy(e => e.Department)");
Console.WriteLine("          .Select(g => new { Dept, Count, Avg, Sum, Max, Min })");
Console.WriteLine("          .OrderBy(g => g.Department).ToList()");
Console.WriteLine("  SQL  : SELECT Department, COUNT(*), AVG(Salary), SUM(Salary),");
Console.WriteLine("                MAX(Salary), MIN(Salary)");
Console.WriteLine("         FROM Employees GROUP BY Department\n");

var groups = service.GroupEmployeesByDepartment();
Console.WriteLine($"  {"Department",-12} {"Count",-7} {"Avg Salary",-14} {"Total",-12} {"Highest",-12} {"Lowest"}");
Console.WriteLine($"  {"----------",-12} {"-----",-7} {"----------",-14} {"-----",-12} {"-------",-12} {"------"}");
foreach (var g in groups)
    Console.WriteLine($"  {g.Department,-12} {g.EmployeeCount,-7} {g.AverageSalary,-14:C0} {g.TotalSalary,-12:C0} {g.HighestSalary,-12:C0} {g.LowestSalary:C0}");

// Expected Output:
//   Department   Count   Avg Salary    Total         Highest       Lowest
//   Finance      2       $46,500       $93,000       $48,000       $45,000
//   HR           3       $37,667       $113,000      $40,000       $35,000
//   IT           3       $55,000       $165,000      $60,000       $50,000

Console.WriteLine("\n╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  Day 5 Complete! All 9 LINQ methods demonstrated ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
