using Microsoft.EntityFrameworkCore;
using EmployeeService.Models;

namespace EmployeeService.Data
{
    /// <summary>
    /// Database context for the Employee microservice.
    /// Owns only the Employees table — no Identity or Department tables.
    /// </summary>
    public class EmployeeDbContext : DbContext
    {
        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed sample employees
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    EmployeeId = 1,
                    FirstName = "Alice",
                    LastName = "Johnson",
                    Email = "alice.johnson@company.com",
                    Phone = "555-0101",
                    Position = "Senior Developer",
                    Salary = 95000.00m,
                    HireDate = new DateTime(2020, 3, 15),
                    IsActive = true,
                    DepartmentId = 1
                },
                new Employee
                {
                    EmployeeId = 2,
                    FirstName = "Bob",
                    LastName = "Smith",
                    Email = "bob.smith@company.com",
                    Phone = "555-0102",
                    Position = "HR Manager",
                    Salary = 75000.00m,
                    HireDate = new DateTime(2019, 6, 1),
                    IsActive = true,
                    DepartmentId = 2
                },
                new Employee
                {
                    EmployeeId = 3,
                    FirstName = "Carol",
                    LastName = "Williams",
                    Email = "carol.williams@company.com",
                    Phone = "555-0103",
                    Position = "Financial Analyst",
                    Salary = 80000.00m,
                    HireDate = new DateTime(2021, 1, 10),
                    IsActive = true,
                    DepartmentId = 3
                }
            );
        }
    }
}
