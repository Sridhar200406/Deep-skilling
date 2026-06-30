using Day6_Migrations.Data;
using Day6_Migrations.Models;
using Microsoft.EntityFrameworkCore;

using var db = new AppDbContext();

// =============================================================
//  STEP 1: APPLY MIGRATIONS (creates DB + tables if not exist)
// =============================================================
Console.WriteLine("=== STEP 1: Applying Migrations ===");
db.Database.Migrate();   // Runs all pending migrations automatically
Console.WriteLine("  Migrations applied successfully!");
Console.WriteLine($"  Database file: company.db");

// =============================================================
//  STEP 2: VERIFY SEEDED DATA (Data Seeding from migrations)
// =============================================================
Console.WriteLine("\n=== STEP 2: Seeded Data (from Migration) ===");

Console.WriteLine("\n  Departments (seeded):");
var departments = db.Departments.ToList();
foreach (var d in departments)
    Console.WriteLine($"    [{d.Id}] {d.Name,-10} | Location: {d.Location}");

Console.WriteLine("\n  Employees (seeded):");
var employees = db.Employees.Include(e => e.Dept).ToList();
foreach (var e in employees)
    Console.WriteLine($"    [{e.Id}] {e.Name,-10} | {e.Dept?.Name,-10} | Salary: {e.Salary} | Joined: {e.JoinedDate:yyyy-MM-dd}");

// =============================================================
//  STEP 3: CRUD ON DATABASE (using EF Core)
// =============================================================

// ── CREATE ───────────────────────────────────────────────────
Console.WriteLine("\n=== STEP 3a: CREATE - Add new Employee ===");
var newEmp = new Employee
{
    Name         = "Alice",
    Department   = "IT",
    DepartmentId = 1,
    Salary       = 55000,
    JoinedDate   = new DateTime(2023, 7, 1)
};
db.Employees.Add(newEmp);
db.SaveChanges();
Console.WriteLine($"  Added: [{newEmp.Id}] {newEmp.Name} | DeptId: {newEmp.DepartmentId} | Salary: {newEmp.Salary}");

Console.WriteLine("\n  Adding new Department: Marketing");
var newDept = new Department { Name = "Marketing", Location = "Seattle" };
db.Departments.Add(newDept);
db.SaveChanges();
Console.WriteLine($"  Added Dept: [{newDept.Id}] {newDept.Name} | {newDept.Location}");

// ── READ ─────────────────────────────────────────────────────
Console.WriteLine("\n=== STEP 3b: READ - Query from Database ===");

Console.WriteLine("\n  All Employees with Department (using Include/Join):");
var all = db.Employees
             .Include(e => e.Dept)
             .OrderBy(e => e.Name)
             .ToList();
foreach (var e in all)
    Console.WriteLine($"    [{e.Id}] {e.Name,-10} | {e.Dept?.Name,-10} | {e.Salary}");

Console.WriteLine("\n  IT Department Employees:");
var itEmps = db.Employees
                .Where(e => e.DepartmentId == 1)
                .OrderByDescending(e => e.Salary)
                .ToList();
foreach (var e in itEmps)
    Console.WriteLine($"    {e.Name} - {e.Salary}");

Console.WriteLine("\n  Employees grouped by Department (with avg salary):");
var grouped = db.Employees
                 .Include(e => e.Dept)
                 .GroupBy(e => e.Dept!.Name)
                 .Select(g => new
                 {
                     Department = g.Key,
                     Count      = g.Count(),
                     AvgSalary  = g.Average(e => e.Salary)
                 })
                 .ToList();
foreach (var g in grouped)
    Console.WriteLine($"    {g.Department,-10} | Employees: {g.Count} | Avg Salary: {g.AvgSalary:F0}");

// ── UPDATE ───────────────────────────────────────────────────
Console.WriteLine("\n=== STEP 3c: UPDATE - Modify Employee ===");
var toUpdate = db.Employees.FirstOrDefault(e => e.Name == "David");
if (toUpdate != null)
{
    Console.WriteLine($"  Before: {toUpdate.Name} | Salary: {toUpdate.Salary} | Dept: {toUpdate.DepartmentId}");
    toUpdate.Salary       = 48000;
    toUpdate.DepartmentId = 3;
    db.SaveChanges();
    Console.WriteLine($"  After : {toUpdate.Name} | Salary: {toUpdate.Salary} | Dept: {toUpdate.DepartmentId}");
}

Console.WriteLine("\n  Bulk UPDATE: Give IT employees a 5% raise");
var itEmployees = db.Employees.Where(e => e.DepartmentId == 1).ToList();
foreach (var e in itEmployees)
    e.Salary = Math.Round(e.Salary * 1.05m, 0);
db.SaveChanges();
Console.WriteLine($"  Updated {itEmployees.Count} IT employees");

// ── DELETE ───────────────────────────────────────────────────
Console.WriteLine("\n=== STEP 3d: DELETE - Remove Employee ===");
var toDelete = db.Employees.FirstOrDefault(e => e.Name == "Smith");
if (toDelete != null)
{
    db.Employees.Remove(toDelete);
    db.SaveChanges();
    Console.WriteLine($"  Deleted: [{toDelete.Id}] {toDelete.Name}");
}

// =============================================================
//  STEP 4: FINAL STATE
// =============================================================
Console.WriteLine("\n=== STEP 4: Final Database State ===");

Console.WriteLine("\n  Departments:");
foreach (var d in db.Departments.Include(d => d.Employees).ToList())
    Console.WriteLine($"    [{d.Id}] {d.Name,-12} | {d.Location,-15} | Employees: {d.Employees.Count}");

Console.WriteLine("\n  Employees (final):");
foreach (var e in db.Employees.Include(e => e.Dept).OrderBy(e => e.Id).ToList())
    Console.WriteLine($"    [{e.Id}] {e.Name,-10} | {e.Dept?.Name,-10} | Salary: {e.Salary} | Joined: {e.JoinedDate:yyyy-MM-dd}");

// =============================================================
//  STEP 5: MIGRATION INFO
// =============================================================
Console.WriteLine("\n=== STEP 5: Migration Information ===");
Console.WriteLine("\n  Applied Migrations:");
foreach (var m in db.Database.GetAppliedMigrations())
    Console.WriteLine($"    + {m}");

Console.WriteLine("\n  Pending Migrations:");
var pending = db.Database.GetPendingMigrations().ToList();
if (pending.Any())
    foreach (var m in pending) Console.WriteLine($"    - {m}");
else
    Console.WriteLine("    (none - all up to date)");

Console.WriteLine("\n=== Done! Database: company.db ===");
