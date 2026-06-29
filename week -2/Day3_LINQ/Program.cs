using Week2_Day3_LINQ.Models;

List<Employee> employees = new List<Employee>()
{
    new Employee { Id = 1, Name = "John",  Department = "IT",      Salary = 50000 },
    new Employee { Id = 2, Name = "David", Department = "HR",      Salary = 35000 },
    new Employee { Id = 3, Name = "Mary",  Department = "IT",      Salary = 60000 },
    new Employee { Id = 4, Name = "Smith", Department = "Finance", Salary = 45000 },
    new Employee { Id = 5, Name = "James", Department = "HR",      Salary = 40000 }
};

// ─────────────────────────────────────────────
// 1. WHERE – Filter employees with salary > 40000
// ─────────────────────────────────────────────
Console.WriteLine("=== 1. WHERE (Salary > 40000) ===");
var highSalary = employees.Where(e => e.Salary > 40000);
foreach (var emp in highSalary)
    Console.WriteLine($"  {emp.Name} - {emp.Salary}");

// ─────────────────────────────────────────────
// 2. SELECT – Project only Name and Department
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 2. SELECT (Name & Department) ===");
var nameAndDept = employees.Select(e => new { e.Name, e.Department });
foreach (var item in nameAndDept)
    Console.WriteLine($"  {item.Name} -> {item.Department}");

// ─────────────────────────────────────────────
// 3. ORDER BY – Sort by Salary ascending
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 3. ORDER BY Salary (Ascending) ===");
var sortedAsc = employees.OrderBy(e => e.Salary);
foreach (var emp in sortedAsc)
    Console.WriteLine($"  {emp.Name} - {emp.Salary}");

// ─────────────────────────────────────────────
// 4. ORDER BY DESCENDING – Sort by Salary descending
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 4. ORDER BY Salary (Descending) ===");
var sortedDesc = employees.OrderByDescending(e => e.Salary);
foreach (var emp in sortedDesc)
    Console.WriteLine($"  {emp.Name} - {emp.Salary}");

// ─────────────────────────────────────────────
// 5. GROUP BY – Group employees by Department
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 5. GROUP BY Department ===");
var grouped = employees.GroupBy(e => e.Department);
foreach (var group in grouped)
{
    Console.WriteLine($"  Department: {group.Key}");
    foreach (var emp in group)
        Console.WriteLine($"    - {emp.Name} ({emp.Salary})");
}

// ─────────────────────────────────────────────
// 6. COUNT – Total number of employees
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 6. COUNT ===");
int total = employees.Count();
Console.WriteLine($"  Total Employees: {total}");

// ─────────────────────────────────────────────
// 7. SUM – Total salary
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 7. SUM of Salaries ===");
decimal totalSalary = employees.Sum(e => e.Salary);
Console.WriteLine($"  Total Salary: {totalSalary}");

// ─────────────────────────────────────────────
// 8. AVERAGE – Average salary
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 8. AVERAGE Salary ===");
double avgSalary = (double)employees.Average(e => e.Salary);
Console.WriteLine($"  Average Salary: {avgSalary}");

// ─────────────────────────────────────────────
// 9. MIN / MAX – Lowest and highest salary
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 9. MIN & MAX Salary ===");
decimal minSalary = employees.Min(e => e.Salary);
decimal maxSalary = employees.Max(e => e.Salary);
Console.WriteLine($"  Min Salary: {minSalary}");
Console.WriteLine($"  Max Salary: {maxSalary}");

// ─────────────────────────────────────────────
// 10. FIRST / LAST – First and last employee
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 10. FIRST & LAST ===");
var first = employees.First();
var last  = employees.Last();
Console.WriteLine($"  First: {first.Name}");
Console.WriteLine($"  Last : {last.Name}");

// ─────────────────────────────────────────────
// 11. ANY / ALL – Condition checks
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 11. ANY & ALL ===");
bool anyIT      = employees.Any(e => e.Department == "IT");
bool allAbove30K = employees.All(e => e.Salary > 30000);
Console.WriteLine($"  Any in IT dept    : {anyIT}");
Console.WriteLine($"  All salary > 30000: {allAbove30K}");

// ─────────────────────────────────────────────
// 12. DISTINCT – Unique departments
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 12. DISTINCT Departments ===");
var departments = employees.Select(e => e.Department).Distinct();
foreach (var dept in departments)
    Console.WriteLine($"  {dept}");

// ─────────────────────────────────────────────
// 13. SKIP & TAKE – Pagination
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 13. SKIP(1) & TAKE(3) ===");
var paged = employees.Skip(1).Take(3);
foreach (var emp in paged)
    Console.WriteLine($"  {emp.Name}");

// ─────────────────────────────────────────────
// 14. WHERE + SELECT (Chaining)
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 14. WHERE + SELECT (Chained) ===");
var itNames = employees
    .Where(e => e.Department == "IT")
    .Select(e => e.Name);
foreach (var name in itNames)
    Console.WriteLine($"  {name}");

// ─────────────────────────────────────────────
// 15. QUERY SYNTAX (SQL-style)
// ─────────────────────────────────────────────
Console.WriteLine("\n=== 15. QUERY SYNTAX (HR dept, ordered by Name) ===");
var query = from emp in employees
            where emp.Department == "HR"
            orderby emp.Name
            select emp;
foreach (var emp in query)
    Console.WriteLine($"  {emp.Name} - {emp.Salary}");
