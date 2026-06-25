using Week2_Day2_EFCoreCRUD.Data;
using Week2_Day2_EFCoreCRUD.Models;

// ============================================================
//  EF Core CRUD Application – Week 2 Day 2
//  Demonstrates: Create, Read, Update, Delete using EF Core
//  Database: SQL Server (local) → EFCoreEmployeeDB
// ============================================================

Console.WriteLine("========================================");
Console.WriteLine("     EF Core CRUD Application");
Console.WriteLine("========================================\n");

// Create a scoped instance of the DbContext (disposed at end of using block)
using AppDbContext db = new AppDbContext();

// Ensure the database and tables are created if they don't exist yet
db.Database.EnsureCreated();
Console.WriteLine("Database ready.\n");

// ----------------------------------------------------------------
// CREATE – Add new employee records to the database
// ----------------------------------------------------------------
Console.WriteLine("--- CREATE ---");

var emp1 = new Employee
{
    Name       = "Alice Johnson",
    Department = "Engineering",
    Salary     = 85000
};

var emp2 = new Employee
{
    Name       = "Bob Smith",
    Department = "Marketing",
    Salary     = 62000
};

var emp3 = new Employee
{
    Name       = "Carol White",
    Department = "Engineering",
    Salary     = 91000
};

// AddRange adds all three in one call; SaveChanges commits to DB
db.Employees.AddRange(emp1, emp2, emp3);
db.SaveChanges();

Console.WriteLine("3 employees inserted successfully.\n");

// ----------------------------------------------------------------
// READ – Retrieve all employees from the database
// ----------------------------------------------------------------
Console.WriteLine("--- READ ALL ---");
PrintEmployees(db);

// ----------------------------------------------------------------
// UPDATE – Modify an existing employee's details
// ----------------------------------------------------------------
Console.WriteLine("--- UPDATE ---");

// Find Bob Smith and update his department and salary
var employeeToUpdate = db.Employees.FirstOrDefault(e => e.Name == "Bob Smith");

if (employeeToUpdate != null)
{
    employeeToUpdate.Department = "Sales";
    employeeToUpdate.Salary     = 70000;
    db.SaveChanges();  // Persist the change

    Console.WriteLine($"Updated: {employeeToUpdate.Name} → " +
                      $"Department: {employeeToUpdate.Department}, " +
                      $"Salary: ${employeeToUpdate.Salary:N0}");
}
else
{
    Console.WriteLine("Employee not found for update.");
}

Console.WriteLine();

// READ again to confirm the update
Console.WriteLine("--- READ AFTER UPDATE ---");
PrintEmployees(db);

// ----------------------------------------------------------------
// DELETE – Remove an employee from the database
// ----------------------------------------------------------------
Console.WriteLine("--- DELETE ---");

// Find Carol White and remove her record
var employeeToDelete = db.Employees.FirstOrDefault(e => e.Name == "Carol White");

if (employeeToDelete != null)
{
    db.Employees.Remove(employeeToDelete);
    db.SaveChanges();  // Persist the deletion

    Console.WriteLine($"Deleted: {employeeToDelete.Name}\n");
}
else
{
    Console.WriteLine("Employee not found for deletion.\n");
}

// Final READ to confirm deletion
Console.WriteLine("--- FINAL READ ---");
PrintEmployees(db);

Console.WriteLine("========================================");
Console.WriteLine("         All CRUD operations done.");
Console.WriteLine("========================================");


// ----------------------------------------------------------------
// Helper method – prints all employees in a formatted table
// ----------------------------------------------------------------
static void PrintEmployees(AppDbContext context)
{
    var employees = context.Employees.ToList();

    if (employees.Count == 0)
    {
        Console.WriteLine("  (no records found)\n");
        return;
    }

    Console.WriteLine($"  {"ID",-5} {"Name",-20} {"Department",-15} {"Salary",10}");
    Console.WriteLine($"  {new string('-', 55)}");

    foreach (var e in employees)
    {
        Console.WriteLine($"  {e.EmployeeId,-5} {e.Name,-20} {e.Department,-15} ${e.Salary,9:N0}");
    }

    Console.WriteLine();
}
