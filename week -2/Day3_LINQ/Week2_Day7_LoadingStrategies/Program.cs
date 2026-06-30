using Microsoft.EntityFrameworkCore;
using Week2_Day7_LoadingStrategies.Data;
using Week2_Day7_LoadingStrategies.Models;
using Week2_Day7_LoadingStrategies.Services;

// ── Bootstrap ────────────────────────────────────────────────────────
using var db = new AppDbContext();
db.Database.EnsureCreated();      // creates company_day7.db + applies seeds
var svc = new EmployeeService(db);

void Header(string title)
{
    Console.WriteLine($"\n{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}{'=',0}");
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('=', 56));
}

void PrintEmployee(Employee e, bool showDept = false)
{
    string dept = showDept && e.Dept != null
        ? $"{e.Dept.Name,-10} | {e.Dept.Location}"
        : e.Department;
    Console.WriteLine($"  [{e.EmployeeId}] {e.Name,-10} | {dept,-20} | {e.Salary:C0} | {e.JoinedDate:yyyy-MM-dd}");
}

Console.WriteLine("Week 2 - Day 7: EF Core Loading Strategies & Performance");
Console.WriteLine("Employee Management System - Complete Edition");

// =============================================================
//  SECTION 1 — EAGER LOADING
// =============================================================
Header("SECTION 1: EAGER LOADING via Include()");
Console.WriteLine("  WHEN: Use when you KNOW related data is needed upfront.");
Console.WriteLine("  HOW : .Include(e => e.Dept) — adds SQL JOIN.");
Console.WriteLine("  SQL : SELECT e.*, d.* FROM Employees e");
Console.WriteLine("        INNER JOIN Departments d ON e.DepartmentId = d.Id\n");

// 1A: All employees with their department (single JOIN query)
Console.WriteLine("  [1A] All Employees + Department (Eager Load):");
Console.WriteLine($"  {"ID",-4} {"Name",-10} {"Dept",-10} {"Location",-14} {"Salary",8} {"Joined"}");
Console.WriteLine($"  {"--",-4} {"----",-10} {"----",-10} {"--------",-14} {"------",8} {"------"}");
var eagerAll = svc.GetAllEmployeesWithDept_EagerLoading();
foreach (var e in eagerAll)
    Console.WriteLine($"  {e.EmployeeId,-4} {e.Name,-10} {e.Dept?.Name,-10} {e.Dept?.Location,-14} {e.Salary,8:C0} {e.JoinedDate:yyyy-MM-dd}");

// Expected Output:
//   1    John       IT         New York       $50,000  2020-01-15
//   2    David      HR         Chicago        $35,000  2019-03-10
//   3    Mary       IT         New York       $60,000  2021-06-01
//   ...

// 1B: Single employee with department by ID
Console.WriteLine("\n  [1B] Single Employee by ID=3 (Eager Load):");
var emp3 = svc.GetEmployeeWithDeptById_EagerLoading(3);
if (emp3 != null)
    Console.WriteLine($"  ID={emp3.EmployeeId} | {emp3.Name} | Dept={emp3.Dept?.Name} | Location={emp3.Dept?.Location} | Salary={emp3.Salary:C0}");

// Expected Output:
//   ID=3 | Mary | Dept=IT | Location=New York | Salary=$60,000

// 1C: Departments with all their employees (reverse Include)
Console.WriteLine("\n  [1C] All Departments with their Employees (Eager Load):");
var depts = svc.GetAllDepartmentsWithEmployees_EagerLoading();
foreach (var d in depts)
{
    Console.WriteLine($"\n  Dept: {d.Name} | Location: {d.Location} | Employees: {d.Employees.Count}");
    foreach (var e in d.Employees)
        Console.WriteLine($"    -> [{e.EmployeeId}] {e.Name} | {e.Salary:C0}");
}

// Expected Output:
//   Dept: Finance | Location: Los Angeles | Employees: 1
//     -> [4] Smith | $45,000
//   Dept: HR      | Location: Chicago     | Employees: 2
//     -> [2] David | $35,000
//     -> [5] James | $40,000
//   Dept: IT      | Location: New York    | Employees: 3
//     -> [1] John  | $50,000
//     -> [3] Mary  | $60,000
//     -> [6] Alice | $55,000

// =============================================================
//  SECTION 2 — EXPLICIT LOADING
// =============================================================
Header("SECTION 2: EXPLICIT LOADING via Entry()");
Console.WriteLine("  WHEN: Use when you conditionally need related data.");
Console.WriteLine("  HOW : Load entity first, then call .Reference().Load()");
Console.WriteLine("        or .Collection().Load() on demand.");
Console.WriteLine("  SQL : 2 separate queries — load entity, then load related.\n");

// 2A: Load employee, then explicitly load its Department
Console.WriteLine("  [2A] Load Employee ID=1, then explicitly load Dept:");
var explicitEmp = svc.GetEmployeeWithExplicitLoading(1);
if (explicitEmp != null)
{
    Console.WriteLine($"  Employee : [{explicitEmp.EmployeeId}] {explicitEmp.Name} | Salary={explicitEmp.Salary:C0}");
    Console.WriteLine($"  Dept     : {explicitEmp.Dept?.Name} | Location={explicitEmp.Dept?.Location}");
}

// Expected Output:
//   Employee : [1] John | Salary=$50,000
//   Dept     : IT | Location=New York

// 2B: Load Department, then explicitly load its Employees
Console.WriteLine("\n  [2B] Load Department ID=1, then explicitly load Employees:");
var explicitDept = svc.GetDepartmentWithExplicitEmployees(1);
if (explicitDept != null)
{
    Console.WriteLine($"  Department: {explicitDept.Name} | {explicitDept.Location}");
    Console.WriteLine($"  Employees ({explicitDept.Employees.Count}):");
    foreach (var e in explicitDept.Employees)
        Console.WriteLine($"    [{e.EmployeeId}] {e.Name} | {e.Salary:C0}");
}

// Expected Output:
//   Department: IT | New York
//   Employees (3):
//     [1] John  | $50,000
//     [3] Mary  | $60,000
//     [6] Alice | $55,000

// =============================================================
//  SECTION 3 — LAZY LOADING
// =============================================================
Header("SECTION 3: LAZY LOADING via Virtual Properties");
Console.WriteLine("  WHEN: Use when you can't predict at query time whether");
Console.WriteLine("        related data will be needed.");
Console.WriteLine("  HOW : UseLazyLoadingProxies() + virtual nav properties.");
Console.WriteLine("        EF Proxy fires SQL automatically on first access.");
Console.WriteLine("  WARN: N+1 problem — 1 extra query per employee in a loop.\n");

Console.WriteLine("  [3A] Load employees (no Include), access Dept lazily:");
var lazyEmps = svc.GetAllEmployees_LazyLoading();
Console.WriteLine($"  {"ID",-4} {"Name",-10} {"Dept (lazy)",-12} {"Location"}");
Console.WriteLine($"  {"--",-4} {"----",-10} {"-----------",-12} {"--------"}");
foreach (var e in lazyEmps)
{
    // Accessing e.Dept here triggers a new SQL query for EACH employee
    // (N+1 problem: 1 query for employees list + N queries for depts)
    Console.WriteLine($"  {e.EmployeeId,-4} {e.Name,-10} {e.Dept?.Name,-12} {e.Dept?.Location}");
}

// Expected Output:
//   (Dept loaded automatically per row — same data as Eager Load but N+1 queries)
//   1    John       IT           New York
//   2    David      HR           Chicago
//   3    Mary       IT           New York
//   4    Smith      Finance      Los Angeles
//   5    James      HR           Chicago
//   6    Alice      IT           New York

Console.WriteLine("\n  NOTE: Lazy Loading fired one SQL query per employee above.");
Console.WriteLine("        Use Eager Loading (Include) in loops to avoid N+1.");

// =============================================================
//  SECTION 4 — AsNoTracking
// =============================================================
Header("SECTION 4: AsNoTracking() for Read-Only Queries");
Console.WriteLine("  WHEN: Display/reporting scenarios where you won't update.");
Console.WriteLine("  HOW : .AsNoTracking() before .ToList()");
Console.WriteLine("  WHY : EF Core skips the change tracker — faster + less RAM.");
Console.WriteLine("        Typically 20-30% faster on large result sets.\n");

Console.WriteLine("  [4A] All employees (AsNoTracking + Include):");
var noTrackingAll = svc.GetAllEmployees_AsNoTracking();
Console.WriteLine($"  Loaded {noTrackingAll.Count} employees (no tracking):");
foreach (var e in noTrackingAll)
    Console.WriteLine($"  [{e.EmployeeId}] {e.Name,-10} | {e.Dept?.Name,-10} | {e.Salary:C0}");

// Expected Output:
//   Loaded 6 employees (no tracking):
//   [1] John       | IT         | $50,000
//   [2] David      | HR         | $35,000
//   ...

Console.WriteLine("\n  [4B] IT employees (AsNoTracking + Where + Include):");
var noTrackingIT = svc.GetEmployeesByDept_AsNoTracking("IT");
foreach (var e in noTrackingIT)
    Console.WriteLine($"  [{e.EmployeeId}] {e.Name} | {e.Dept?.Name} | {e.Dept?.Location} | {e.Salary:C0}");

// Expected Output:
//   [6] Alice | IT | New York | $55,000
//   [1] John  | IT | New York | $50,000
//   [3] Mary  | IT | New York | $60,000

// =============================================================
//  SECTION 5 — PERFORMANCE OPTIMIZATION
// =============================================================
Header("SECTION 5: Performance Optimization");

// 5A: Projection — Select only needed columns
Console.WriteLine("  [5A] SELECT Projection (only needed columns):");
Console.WriteLine("  SQL : SELECT EmployeeId, Name, Department, Salary, d.Location");
Console.WriteLine("        FROM Employees e JOIN Departments d ON e.DepartmentId=d.Id\n");
var projections = svc.GetEmployeeProjections();
Console.WriteLine($"  {"ID",-4} {"Name",-10} {"Dept",-10} {"Salary",8} {"Location"}");
Console.WriteLine($"  {"--",-4} {"----",-10} {"----",-10} {"------",8} {"--------"}");
foreach (var p in projections)
    Console.WriteLine($"  {p.EmployeeId,-4} {p.Name,-10} {p.Department,-10} {p.Salary,8:C0} {p.DeptLocation}");

// Expected Output:
//   ID   Name       Dept        Salary   Location
//   1    John       IT          $50,000  New York
//   2    David      HR          $35,000  Chicago
//   ...

// 5B: Filter pushed to DB — salary range
Console.WriteLine("\n  [5B] WHERE pushed to SQL — Salary between $40,000 and $55,000:");
var rangeEmps = svc.GetEmployeesInSalaryRange(40000, 55000);
foreach (var e in rangeEmps)
    Console.WriteLine($"  [{e.EmployeeId}] {e.Name,-10} | {e.Dept?.Name,-10} | {e.Salary:C0}");

// Expected Output:
//   [5] James      | HR         | $40,000
//   [4] Smith      | Finance    | $45,000
//   [1] John       | IT         | $50,000
//   [6] Alice      | IT         | $55,000

// 5C: OrderBy + Take — Top 3 highest paid
Console.WriteLine("\n  [5C] TOP 3 Highest Paid (OrderBy + Take):");
Console.WriteLine("  SQL : SELECT TOP 3 * FROM Employees ORDER BY Salary DESC\n");
var top3 = svc.GetTopNHighestPaid(3);
for (int i = 0; i < top3.Count; i++)
    Console.WriteLine($"  #{i+1} [{top3[i].EmployeeId}] {top3[i].Name,-10} | {top3[i].Dept?.Name,-10} | {top3[i].Salary:C0}");

// Expected Output:
//   #1 [3] Mary       | IT         | $60,000
//   #2 [6] Alice      | IT         | $55,000
//   #3 [1] John       | IT         | $50,000

// 5D: FirstOrDefault — single row, stops at first match
Console.WriteLine("\n  [5D] FirstOrDefault — Employee ID=2 with Dept:");
var found = svc.FindEmployeeWithDept(2);
if (found != null)
    Console.WriteLine($"  [{found.EmployeeId}] {found.Name} | {found.Dept?.Name} | {found.Dept?.Location} | {found.Salary:C0}");

// Expected Output:
//   [2] David | HR | Chicago | $35,000

// 5E: Full optimized pipeline — Where + Select + OrderBy + Take
Console.WriteLine("\n  [5E] Full Pipeline — Top 2 IT earners (Where+Select+OrderBy+Take):");
Console.WriteLine("  SQL : SELECT TOP 2 EmployeeId, Name, Dept, Salary, Location");
Console.WriteLine("        FROM Employees e JOIN Departments d ...");
Console.WriteLine("        WHERE Department='IT' ORDER BY Salary DESC\n");
var top2IT = svc.GetOptimizedTopEarnersByDept("IT", 2);
foreach (var p in top2IT)
    Console.WriteLine($"  [{p.EmployeeId}] {p.Name,-10} | {p.Department,-8} | {p.Salary:C0} | {p.DeptLocation}");

// Expected Output:
//   [3] Mary       | IT       | $60,000 | New York
//   [6] Alice      | IT       | $55,000 | New York

// =============================================================
//  SECTION 6 — COMPARISON SUMMARY
// =============================================================
Header("SECTION 6: Loading Strategy Comparison");
Console.WriteLine();
Console.WriteLine($"  {"Strategy",-20} {"SQL Queries",-14} {"Best Use Case"}");
Console.WriteLine($"  {"--------",-20} {"-----------",-14} {"-------------"}");
Console.WriteLine($"  {"Eager Loading",-20} {"1 (JOIN)",-14} {"Always need related data"}");
Console.WriteLine($"  {"Explicit Loading",-20} {"2 (separate)",-14} {"Conditionally need related data"}");
Console.WriteLine($"  {"Lazy Loading",-20} {"N+1 (auto)",-14} {"Unpredictable access patterns"}");
Console.WriteLine($"  {"AsNoTracking",-20} {"1 (no track)",-14} {"Read-only, display, reporting"}");
Console.WriteLine();
Console.WriteLine("  BEST PRACTICES:");
Console.WriteLine("  1. Prefer Eager Loading (Include) for most scenarios.");
Console.WriteLine("  2. Use AsNoTracking() on all read-only queries.");
Console.WriteLine("  3. Use Select() projection — never SELECT * when avoidable.");
Console.WriteLine("  4. Use Where() in LINQ query — never filter in memory.");
Console.WriteLine("  5. Use Take() for pagination — never load all rows.");
Console.WriteLine("  6. Use FirstOrDefault() instead of ToList().First().");
Console.WriteLine("  7. Avoid Lazy Loading in loops — causes N+1 queries.");
Console.WriteLine("  8. Use Explicit Loading for optional/conditional relations.");

Console.WriteLine("\nDay 7 Complete! All loading strategies demonstrated.");
