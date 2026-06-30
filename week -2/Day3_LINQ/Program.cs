using Week2_Day3_LINQ.Models;

// ─────────────────────────────────────────────
// Initial Data
// ─────────────────────────────────────────────
List<Employee> employees = new List<Employee>()
{
    new Employee { Id = 1, Name = "John",  Department = "IT",      Salary = 50000 },
    new Employee { Id = 2, Name = "David", Department = "HR",      Salary = 35000 },
    new Employee { Id = 3, Name = "Mary",  Department = "IT",      Salary = 60000 },
    new Employee { Id = 4, Name = "Smith", Department = "Finance", Salary = 45000 },
    new Employee { Id = 5, Name = "James", Department = "HR",      Salary = 40000 }
};

// Helper: Print all employees
void PrintAll(string title)
{
    Console.WriteLine($"\n--- {title} ---");
    foreach (var e in employees)
        Console.WriteLine($"  [{e.Id}] {e.Name,-10} | {e.Department,-10} | {e.Salary}");
}

// =============================================================
//  C R U D   O P E R A T I O N S
// =============================================================

// ── CREATE ──────────────────────────────────────────────────
Console.WriteLine("*** C - CREATE ***");
PrintAll("Before CREATE");
Employee newEmp = new Employee
{
    Id         = employees.Max(e => e.Id) + 1,
    Name       = "Alice",
    Department = "IT",
    Salary     = 55000
};
employees.Add(newEmp);
Console.WriteLine($"\n  Added: [{newEmp.Id}] {newEmp.Name} | {newEmp.Department} | {newEmp.Salary}");
PrintAll("After CREATE");

// ── READ ─────────────────────────────────────────────────────
Console.WriteLine("\n*** R - READ ***");

Console.WriteLine("\n  [R1] All Employees:");
foreach (var emp in employees)
    Console.WriteLine($"    [{emp.Id}] {emp.Name,-10} | {emp.Department,-10} | {emp.Salary}");

var byId = employees.FirstOrDefault(e => e.Id == 3);
Console.WriteLine($"\n  [R2] Find by Id=3: {byId?.Name} | {byId?.Department} | {byId?.Salary}");

Console.WriteLine("\n  [R3] Filter by Department = IT:");
foreach (var emp in employees.Where(e => e.Department == "IT"))
    Console.WriteLine($"    [{emp.Id}] {emp.Name} - {emp.Salary}");

Console.WriteLine("\n  [R4] Sorted by Salary (High to Low):");
foreach (var emp in employees.OrderByDescending(e => e.Salary))
    Console.WriteLine($"    [{emp.Id}] {emp.Name,-10} - {emp.Salary}");

// ── UPDATE ───────────────────────────────────────────────────
Console.WriteLine("\n*** U - UPDATE ***");
PrintAll("Before UPDATE");
var toUpdate = employees.FirstOrDefault(e => e.Id == 2);
if (toUpdate != null)
{
    Console.WriteLine($"\n  Updating [{toUpdate.Id}] {toUpdate.Name}: Salary {toUpdate.Salary}->45000, Dept {toUpdate.Department}->Finance");
    toUpdate.Salary     = 45000;
    toUpdate.Department = "Finance";
}
PrintAll("After UPDATE");

// ── DELETE ───────────────────────────────────────────────────
Console.WriteLine("\n*** D - DELETE ***");
PrintAll("Before DELETE");
var toDelete = employees.FirstOrDefault(e => e.Id == 4);
if (toDelete != null)
{
    employees.Remove(toDelete);
    Console.WriteLine($"\n  Deleted: [{toDelete.Id}] {toDelete.Name}");
}
PrintAll("After DELETE");

// =============================================================
//  L I N Q   Q U E R I E S
// =============================================================
Console.WriteLine("\n\n=============================================================");
Console.WriteLine("              L I N Q   Q U E R I E S");
Console.WriteLine("=============================================================");

// ── BASIC QUERIES ────────────────────────────────────────────

Console.WriteLine("\n--- BASIC QUERIES ---");

// Q1. WHERE - filter
Console.WriteLine("\nQ1. WHERE: Salary > 40000");
foreach (var e in employees.Where(e => e.Salary > 40000))
    Console.WriteLine($"  {e.Name} - {e.Salary}");

// Q2. SELECT - projection
Console.WriteLine("\nQ2. SELECT: Names only");
foreach (var name in employees.Select(e => e.Name))
    Console.WriteLine($"  {name}");

// Q3. SELECT anonymous type
Console.WriteLine("\nQ3. SELECT: Anonymous type (Name + Dept)");
foreach (var item in employees.Select(e => new { e.Name, e.Department }))
    Console.WriteLine($"  {item.Name} -> {item.Department}");

// Q4. ORDER BY ascending
Console.WriteLine("\nQ4. ORDER BY Salary (Asc)");
foreach (var e in employees.OrderBy(e => e.Salary))
    Console.WriteLine($"  {e.Name} - {e.Salary}");

// Q5. ORDER BY descending
Console.WriteLine("\nQ5. ORDER BY Salary (Desc)");
foreach (var e in employees.OrderByDescending(e => e.Salary))
    Console.WriteLine($"  {e.Name} - {e.Salary}");

// Q6. ORDER BY multiple fields
Console.WriteLine("\nQ6. ORDER BY Dept then Salary");
foreach (var e in employees.OrderBy(e => e.Department).ThenByDescending(e => e.Salary))
    Console.WriteLine($"  {e.Department,-10} | {e.Name} - {e.Salary}");

// ── AGGREGATION QUERIES ──────────────────────────────────────

Console.WriteLine("\n--- AGGREGATION QUERIES ---");

// Q7. COUNT
Console.WriteLine($"\nQ7.  COUNT  : Total = {employees.Count()}");
Console.WriteLine($"     COUNT  : IT dept = {employees.Count(e => e.Department == "IT")}");

// Q8. SUM
Console.WriteLine($"\nQ8.  SUM    : Total Salary = {employees.Sum(e => e.Salary)}");

// Q9. AVERAGE
Console.WriteLine($"\nQ9.  AVG    : Average Salary = {employees.Average(e => e.Salary):F2}");

// Q10. MIN / MAX
Console.WriteLine($"\nQ10. MIN    : {employees.Min(e => e.Salary)}");
Console.WriteLine($"     MAX    : {employees.Max(e => e.Salary)}");

// Q11. MIN/MAX employee (not just value)
var richest = employees.OrderByDescending(e => e.Salary).First();
var poorest = employees.OrderBy(e => e.Salary).First();
Console.WriteLine($"\nQ11. Highest Paid : {richest.Name} ({richest.Salary})");
Console.WriteLine($"     Lowest  Paid : {poorest.Name} ({poorest.Salary})");

// ── FILTERING QUERIES ────────────────────────────────────────

Console.WriteLine("\n--- FILTERING QUERIES ---");

// Q12. WHERE + multiple conditions
Console.WriteLine("\nQ12. WHERE: IT dept AND Salary >= 50000");
foreach (var e in employees.Where(e => e.Department == "IT" && e.Salary >= 50000))
    Console.WriteLine($"  {e.Name} - {e.Salary}");

// Q13. WHERE with OR
Console.WriteLine("\nQ13. WHERE: HR OR Finance dept");
foreach (var e in employees.Where(e => e.Department == "HR" || e.Department == "Finance"))
    Console.WriteLine($"  {e.Name} | {e.Department}");

// Q14. WHERE contains (name search)
Console.WriteLine("\nQ14. WHERE: Name contains 'a' (case-insensitive)");
foreach (var e in employees.Where(e => e.Name.Contains("a", StringComparison.OrdinalIgnoreCase)))
    Console.WriteLine($"  {e.Name}");

// Q15. FIRST / FIRSTORDEFAULT
Console.WriteLine("\nQ15. FIRST in Finance dept:");
var firstFinance = employees.FirstOrDefault(e => e.Department == "Finance");
Console.WriteLine(firstFinance != null ? $"  {firstFinance.Name}" : "  None found");

// Q16. LAST / LASTORDEFAULT
Console.WriteLine("\nQ16. LAST employee added:");
Console.WriteLine($"  {employees.Last().Name}");

// Q17. SINGLE (exactly one match)
Console.WriteLine("\nQ17. SINGLE employee with Id=1:");
var single = employees.SingleOrDefault(e => e.Id == 1);
Console.WriteLine($"  {single?.Name}");

// ── GROUPING QUERIES ─────────────────────────────────────────

Console.WriteLine("\n--- GROUPING QUERIES ---");

// Q18. GROUP BY department
Console.WriteLine("\nQ18. GROUP BY Department:");
foreach (var g in employees.GroupBy(e => e.Department))
{
    Console.WriteLine($"  [{g.Key}] - {g.Count()} employee(s)");
    foreach (var e in g)
        Console.WriteLine($"    {e.Name} - {e.Salary}");
}

// Q19. GROUP BY + aggregate per group
Console.WriteLine("\nQ19. GROUP BY Dept - Total & Avg Salary per dept:");
foreach (var g in employees.GroupBy(e => e.Department))
    Console.WriteLine($"  {g.Key,-10} | Count: {g.Count()} | Total: {g.Sum(e => e.Salary)} | Avg: {g.Average(e => e.Salary):F0}");

// Q20. GROUP BY with HAVING (filter groups)
Console.WriteLine("\nQ20. Departments with more than 1 employee:");
foreach (var g in employees.GroupBy(e => e.Department).Where(g => g.Count() > 1))
    Console.WriteLine($"  {g.Key} - {g.Count()} employees");

// ── PROJECTION & TRANSFORMATION ──────────────────────────────

Console.WriteLine("\n--- PROJECTION & TRANSFORMATION ---");

// Q21. SELECT with computed field
Console.WriteLine("\nQ21. SELECT with computed Bonus (10% of Salary):");
foreach (var item in employees.Select(e => new { e.Name, e.Salary, Bonus = e.Salary * 0.10m }))
    Console.WriteLine($"  {item.Name,-10} | Salary: {item.Salary} | Bonus: {item.Bonus}");

// Q22. SELECT with index
Console.WriteLine("\nQ22. SELECT with Row Number:");
foreach (var item in employees.Select((e, i) => new { Index = i + 1, e.Name, e.Department }))
    Console.WriteLine($"  {item.Index}. {item.Name} ({item.Department})");

// Q23. DISTINCT
Console.WriteLine("\nQ23. DISTINCT Departments:");
foreach (var dept in employees.Select(e => e.Department).Distinct().OrderBy(d => d))
    Console.WriteLine($"  {dept}");

// ── PAGING QUERIES ───────────────────────────────────────────

Console.WriteLine("\n--- PAGING QUERIES ---");

// Q24. SKIP & TAKE (Page 1)
Console.WriteLine("\nQ24. Page 1 (Take 3):");
foreach (var e in employees.Take(3))
    Console.WriteLine($"  {e.Name}");

// Q25. SKIP & TAKE (Page 2)
Console.WriteLine("\nQ25. Page 2 (Skip 3, Take 3):");
foreach (var e in employees.Skip(3).Take(3))
    Console.WriteLine($"  {e.Name}");

// Q26. TAKE WHILE
Console.WriteLine("\nQ26. TAKE WHILE Salary <= 55000 (ordered asc):");
foreach (var e in employees.OrderBy(e => e.Salary).TakeWhile(e => e.Salary <= 55000))
    Console.WriteLine($"  {e.Name} - {e.Salary}");

// Q27. SKIP WHILE
Console.WriteLine("\nQ27. SKIP WHILE Salary < 50000 (ordered asc):");
foreach (var e in employees.OrderBy(e => e.Salary).SkipWhile(e => e.Salary < 50000))
    Console.WriteLine($"  {e.Name} - {e.Salary}");

// ── SET QUERIES ──────────────────────────────────────────────

Console.WriteLine("\n--- SET QUERIES ---");

List<string> itNames = employees.Where(e => e.Department == "IT").Select(e => e.Name).ToList();
List<string> allNames = employees.Select(e => e.Name).ToList();

// Q28. UNION
Console.WriteLine("\nQ28. UNION (IT names + all names, distinct):");
foreach (var n in itNames.Union(allNames))
    Console.WriteLine($"  {n}");

// Q29. INTERSECT
Console.WriteLine("\nQ29. INTERSECT (names in IT that also exist in full list):");
foreach (var n in itNames.Intersect(allNames))
    Console.WriteLine($"  {n}");

// Q30. EXCEPT
List<string> hrNames = employees.Where(e => e.Department == "HR").Select(e => e.Name).ToList();
Console.WriteLine("\nQ30. EXCEPT (all names NOT in HR):");
foreach (var n in allNames.Except(hrNames))
    Console.WriteLine($"  {n}");

// ── EXISTENCE & QUANTIFIER QUERIES ───────────────────────────

Console.WriteLine("\n--- EXISTENCE & QUANTIFIER QUERIES ---");

// Q31. ANY
Console.WriteLine($"\nQ31. ANY employee in Finance? {employees.Any(e => e.Department == "Finance")}");

// Q32. ALL
Console.WriteLine($"Q32. ALL employees salary > 30000? {employees.All(e => e.Salary > 30000)}");

// Q33. CONTAINS (by value)
Console.WriteLine($"Q33. CONTAINS name 'John'? {employees.Select(e => e.Name).Contains("John")}");

// ── CONVERSION QUERIES ───────────────────────────────────────

Console.WriteLine("\n--- CONVERSION QUERIES ---");

// Q34. ToList / ToArray / ToDictionary
var empList = employees.Where(e => e.Salary > 40000).ToList();
Console.WriteLine($"\nQ34. ToList  (Salary > 40000): {empList.Count} records");

var empArray = employees.ToArray();
Console.WriteLine($"     ToArray : {empArray.Length} records");

var empDict = employees.ToDictionary(e => e.Id, e => e.Name);
Console.WriteLine($"     ToDictionary (Id -> Name):");
foreach (var kv in empDict)
    Console.WriteLine($"       {kv.Key} -> {kv.Value}");

// Q35. ToLookup (like multi-value dictionary)
Console.WriteLine("\nQ35. ToLookup by Department:");
var lookup = employees.ToLookup(e => e.Department);
foreach (var group in lookup)
{
    Console.Write($"  {group.Key}: ");
    Console.WriteLine(string.Join(", ", group.Select(e => e.Name)));
}

// ── QUERY SYNTAX (SQL-style) ──────────────────────────────────

Console.WriteLine("\n--- QUERY SYNTAX (SQL Style) ---");

// Q36. Basic query syntax
Console.WriteLine("\nQ36. Query Syntax - HR dept ordered by Name:");
var q36 = from e in employees
           where e.Department == "HR"
           orderby e.Name
           select e;
foreach (var e in q36)
    Console.WriteLine($"  {e.Name} - {e.Salary}");

// Q37. Query syntax with let (computed variable)
Console.WriteLine("\nQ37. Query Syntax with LET (Yearly Bonus = Salary * 0.15):");
var q37 = from e in employees
           let bonus = e.Salary * 0.15m
           where bonus > 6000
           select new { e.Name, e.Salary, Bonus = bonus };
foreach (var item in q37)
    Console.WriteLine($"  {item.Name,-10} | Salary: {item.Salary} | Bonus: {item.Bonus}");

// Q38. Query syntax with group by
Console.WriteLine("\nQ38. Query Syntax with GROUP BY:");
var q38 = from e in employees
           group e by e.Department into deptGroup
           select new
           {
               Dept    = deptGroup.Key,
               Count   = deptGroup.Count(),
               AvgSal  = deptGroup.Average(e => e.Salary)
           };
foreach (var item in q38)
    Console.WriteLine($"  {item.Dept,-10} | Employees: {item.Count} | Avg Salary: {item.AvgSal:F0}");

// Q39. Query syntax - order by multiple
Console.WriteLine("\nQ39. Query Syntax - Order by Dept then by Name:");
var q39 = from e in employees
           orderby e.Department, e.Name
           select e;
foreach (var e in q39)
    Console.WriteLine($"  {e.Department,-10} | {e.Name}");

// Q40. Query syntax - select into new shape
Console.WriteLine("\nQ40. Query Syntax - Full employee report:");
var q40 = from e in employees
           orderby e.Salary descending
           select new
           {
               Rank       = employees.Count(x => x.Salary > e.Salary) + 1,
               e.Name,
               e.Department,
               e.Salary,
               Grade = e.Salary >= 55000 ? "A" :
                       e.Salary >= 45000 ? "B" : "C"
           };
Console.WriteLine($"  {"Rank",-5} {"Name",-10} {"Dept",-10} {"Salary",-8} Grade");
Console.WriteLine($"  {"----",-5} {"----",-10} {"----",-10} {"------",-8} -----");
foreach (var item in q40)
    Console.WriteLine($"  {item.Rank,-5} {item.Name,-10} {item.Department,-10} {item.Salary,-8} {item.Grade}");
