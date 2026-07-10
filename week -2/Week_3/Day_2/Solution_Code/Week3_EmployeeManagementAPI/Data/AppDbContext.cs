using Microsoft.EntityFrameworkCore;
using Week3_EmployeeManagementAPI.Models;

namespace Week3_EmployeeManagementAPI.Data
{
    /// <summary>
    /// EF Core DbContext — unchanged from Day 1.
    /// Connects to the same SQL Server EmployeeManagementDB database.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Employee → Department: many employees belong to one department
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Departments
            modelBuilder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, DepartmentName = "Engineering",     Description = "Software development and architecture" },
                new Department { DepartmentId = 2, DepartmentName = "Human Resources", Description = "Recruitment and employee welfare" },
                new Department { DepartmentId = 3, DepartmentName = "Finance",         Description = "Accounting and financial planning" },
                new Department { DepartmentId = 4, DepartmentName = "Marketing",       Description = "Brand and product promotion" }
            );

            // Seed Employees
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    EmployeeId   = 1, FirstName = "Alice",  LastName = "Johnson",
                    Email        = "alice.johnson@company.com", Phone = "555-0101",
                    Position     = "Senior Developer", Salary = 95000.00m,
                    HireDate     = new DateTime(2020, 3, 15), IsActive = true, DepartmentId = 1
                },
                new Employee
                {
                    EmployeeId   = 2, FirstName = "Bob",    LastName = "Smith",
                    Email        = "bob.smith@company.com",     Phone = "555-0102",
                    Position     = "HR Manager",      Salary = 75000.00m,
                    HireDate     = new DateTime(2019, 6, 1),  IsActive = true, DepartmentId = 2
                },
                new Employee
                {
                    EmployeeId   = 3, FirstName = "Carol",  LastName = "Williams",
                    Email        = "carol.williams@company.com", Phone = "555-0103",
                    Position     = "Financial Analyst", Salary = 80000.00m,
                    HireDate     = new DateTime(2021, 1, 10), IsActive = true, DepartmentId = 3
                }
            );
        }
    }
}
