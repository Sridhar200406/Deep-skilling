using Week2_Day4_EFCoreCRUD.Data;
using Week2_Day4_EFCoreCRUD.Models;
using Week2_Day4_EFCoreCRUD.Services;

// ──────────────────────────────────────────────────────────────
//  Bootstrap: create DbContext and EmployeeService
// ──────────────────────────────────────────────────────────────
using var context = new AppDbContext();

// EnsureCreated: creates the database and Employees table if they
// do not already exist (uses the schema from OnModelCreating).
context.Database.EnsureCreated();

var service = new EmployeeService(context);

Console.WriteLine("=======================================================");
Console.WriteLine("  Week 2 - Day 4: EF Core CRUD — Employee Management");
Console.WriteLine("=======================================================");

// ──────────────────────────────────────────────────────────────
//  Clean slate: remove any leftover data from previous runs
// ──────────────────────────────────────────────────────────────
var existing = service.GetAllEmployees();
foreach (var old in existing)
    context.Employees.Remove(old);
context.SaveChanges();
Console.WriteLine("\n  [INIT] Cleared existing records for a clean demo.\n");

// =============================================================
//  STEP 1 — CREATE  (AddEmployee)
//  Adds 5 sample employees to the database.
//  EF Core: INSERT INTO Employees (Name, Department, Salary) VALUES (...)
// =============================================================
Console.WriteLine("-------------------------------------------------------");
Console.WriteLine("  STEP 1: CREATE — AddEmployee()");
Console.WriteLine("  Inserts new employee rows into the database.");
Console.WriteLine("-------------------------------------------------------");

try
{
    service.AddEmployee(new Employee { Name = "John",   Department = "IT",      Salary = 50000 });
    service.AddEmployee(new Employee { Name = "David",  Department = "HR",      Salary = 35000 });
    service.AddEmployee(new Employee { Name = "Mary",   Department = "IT",      Salary = 60000 });
    service.AddEmployee(new Employee { Name = "Smith",  Department = "Finance", Salary = 45000 });
    service.AddEmployee(new Employee { Name = "James",  Department = "HR",      Salary = 40000 });
}
catch (Exception ex)
{
    Console.WriteLine($"  [ERROR] {ex.Message}");
}

// Expected Output:
//   [SUCCESS] Employee added: ID=1, Name=John
//   [SUCCESS] Employee added: ID=2, Name=David
//   [SUCCESS] Employee added: ID=3, Name=Mary
//   [SUCCESS] Employee added: ID=4, Name=Smith
//   [SUCCESS] Employee added: ID=5, Name=James

// Validation demo — try adding invalid data
Console.WriteLine("\n  Validation test — empty name:");
try
{
    service.AddEmployee(new Employee { Name = "", Department = "IT", Salary = 30000 });
}
catch (ArgumentException ex)
{
    Console.WriteLine($"  [CAUGHT] {ex.Message}");
}

Console.WriteLine("\n  Validation test — negative salary:");
try
{
    service.AddEmployee(new Employee { Name = "Test", Department = "IT", Salary = -500 });
}
catch (ArgumentException ex)
{
    Console.WriteLine($"  [CAUGHT] {ex.Message}");
}

// =============================================================
//  STEP 2 — READ ALL  (GetAllEmployees)
//  Fetches every row and displays in a formatted table.
//  EF Core: SELECT * FROM Employees ORDER BY EmployeeId ASC
// =============================================================
Console.WriteLine("\n-------------------------------------------------------");
Console.WriteLine("  STEP 2: READ ALL — GetAllEmployees()");
Console.WriteLine("  SQL: SELECT * FROM Employees ORDER BY EmployeeId ASC");
Console.WriteLine("-------------------------------------------------------");

var allEmployees = service.GetAllEmployees();
service.PrintEmployees(allEmployees);

// Expected Output:
//   ID     Name         Department   Salary
//   ------  ------------  ------------  ----------
//   1      John         IT            $50,000
//   2      David        HR            $35,000
//   3      Mary         IT            $60,000
//   4      Smith        Finance       $45,000
//   5      James        HR            $40,000
//   Total: 5 record(s)

// =============================================================
//  STEP 3 — READ BY ID  (GetEmployeeById)
//  Finds a single employee by their primary key.
//  EF Core: SELECT TOP 1 * FROM Employees WHERE EmployeeId = @id
// =============================================================
Console.WriteLine("\n-------------------------------------------------------");
Console.WriteLine("  STEP 3: READ BY ID — GetEmployeeById(3)");
Console.WriteLine("  SQL: SELECT TOP 1 * FROM Employees WHERE EmployeeId = 3");
Console.WriteLine("-------------------------------------------------------");

try
{
    var emp = service.GetEmployeeById(3);
    if (emp != null)
        Console.WriteLine($"  Found  -> ID={emp.EmployeeId} | Name={emp.Name} | Dept={emp.Department} | Salary={emp.Salary:C0}");
}
catch (Exception ex)
{
    Console.WriteLine($"  [ERROR] {ex.Message}");
}

// Expected Output:
//   Found  -> ID=3 | Name=Mary | Dept=IT | Salary=$60,000

// Search for non-existent ID
Console.WriteLine("\n  Searching for non-existent ID=99:");
try
{
    var notFound = service.GetEmployeeById(99);
    if (notFound == null)
        Console.WriteLine("  Result -> null (employee does not exist)");
}
catch (Exception ex)
{
    Console.WriteLine($"  [ERROR] {ex.Message}");
}

// Expected Output:
//   [NOT FOUND] No employee found with ID=99
//   Result -> null (employee does not exist)

// =============================================================
//  STEP 4 — UPDATE  (UpdateEmployee)
//  Updates salary and department for employee ID=2 (David).
//  EF Core: UPDATE Employees SET Department=..., Salary=...
//           WHERE EmployeeId = 2
// =============================================================
Console.WriteLine("\n-------------------------------------------------------");
Console.WriteLine("  STEP 4: UPDATE — UpdateEmployee(2, ...)");
Console.WriteLine("  Updates David: Salary 35000->48000, HR->Finance");
Console.WriteLine("  SQL: UPDATE Employees SET Department='Finance',");
Console.WriteLine("       Salary=48000 WHERE EmployeeId=2");
Console.WriteLine("-------------------------------------------------------");

Console.WriteLine("\n  Before update:");
var beforeUpdate = service.GetEmployeeById(2);
if (beforeUpdate != null)
    Console.WriteLine($"  ID={beforeUpdate.EmployeeId} | {beforeUpdate.Name} | {beforeUpdate.Department} | {beforeUpdate.Salary:C0}");

try
{
    service.UpdateEmployee(2, new Employee
    {
        Name       = "David",
        Department = "Finance",
        Salary     = 48000
    });
}
catch (Exception ex)
{
    Console.WriteLine($"  [ERROR] {ex.Message}");
}

Console.WriteLine("\n  After update:");
var afterUpdate = service.GetEmployeeById(2);
if (afterUpdate != null)
    Console.WriteLine($"  ID={afterUpdate.EmployeeId} | {afterUpdate.Name} | {afterUpdate.Department} | {afterUpdate.Salary:C0}");

// Expected Output:
//   Before update:
//   ID=2 | David | HR | $35,000
//   [SUCCESS] Employee ID=2 updated. Dept: 'HR'->'Finance' Salary: 35000->48000
//   After update:
//   ID=2 | David | Finance | $48,000

// Update non-existent employee
Console.WriteLine("\n  Updating non-existent ID=99:");
try
{
    service.UpdateEmployee(99, new Employee { Name = "Ghost", Department = "IT", Salary = 10000 });
}
catch (KeyNotFoundException ex)
{
    Console.WriteLine($"  [CAUGHT] {ex.Message}");
}

// =============================================================
//  STEP 5 — READ ALL after update (verify change persisted)
// =============================================================
Console.WriteLine("\n-------------------------------------------------------");
Console.WriteLine("  STEP 5: READ ALL after UPDATE");
Console.WriteLine("-------------------------------------------------------");
service.PrintEmployees(service.GetAllEmployees());

// Expected Output: David now shows Finance / $48,000

// =============================================================
//  STEP 6 — DELETE  (DeleteEmployee)
//  Removes employee ID=4 (Smith) from the database.
//  EF Core: DELETE FROM Employees WHERE EmployeeId = 4
// =============================================================
Console.WriteLine("\n-------------------------------------------------------");
Console.WriteLine("  STEP 6: DELETE — DeleteEmployee(4)");
Console.WriteLine("  Removes Smith (Finance) from the database.");
Console.WriteLine("  SQL: DELETE FROM Employees WHERE EmployeeId = 4");
Console.WriteLine("-------------------------------------------------------");

try
{
    service.DeleteEmployee(4);
}
catch (Exception ex)
{
    Console.WriteLine($"  [ERROR] {ex.Message}");
}

// Expected Output:
//   [SUCCESS] Deleted: ID=4, Name=Smith

// Delete non-existent employee
Console.WriteLine("\n  Deleting non-existent ID=99:");
try
{
    service.DeleteEmployee(99);
}
catch (KeyNotFoundException ex)
{
    Console.WriteLine($"  [CAUGHT] {ex.Message}");
}

// =============================================================
//  STEP 7 — READ ALL after DELETE (verify removal persisted)
// =============================================================
Console.WriteLine("\n-------------------------------------------------------");
Console.WriteLine("  STEP 7: READ ALL after DELETE");
Console.WriteLine("  Smith (ID=4) should no longer appear.");
Console.WriteLine("-------------------------------------------------------");
service.PrintEmployees(service.GetAllEmployees());

// Expected Output:
//   ID     Name         Department   Salary
//   1      John         IT            $50,000
//   2      David        Finance       $48,000
//   3      Mary         IT            $60,000
//   5      James        HR            $40,000
//   Total: 4 record(s)

// =============================================================
//  STEP 8 — INVALID ID validation
// =============================================================
Console.WriteLine("\n-------------------------------------------------------");
Console.WriteLine("  STEP 8: Validation — Invalid ID (0 or negative)");
Console.WriteLine("-------------------------------------------------------");
try
{
    service.GetEmployeeById(0);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"  [CAUGHT] GetById(0)    -> {ex.Message}");
}

try
{
    service.DeleteEmployee(-1);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"  [CAUGHT] DeleteById(-1) -> {ex.Message}");
}

// =============================================================
//  DONE
// =============================================================
Console.WriteLine("\n=======================================================");
Console.WriteLine("  Day 4 Complete! All CRUD operations demonstrated.");
Console.WriteLine("=======================================================");
Console.WriteLine("\n  Steps to run:");
Console.WriteLine("  1. Open terminal in Week2_Day4_EFCoreCRUD folder");
Console.WriteLine("  2. dotnet restore");
Console.WriteLine("  3. dotnet run");
Console.WriteLine("  (SQL Server must be running on localhost)");
