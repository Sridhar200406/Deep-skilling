using DepartmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DepartmentService.Data
{
    public class DepartmentDbContext : DbContext
    {
        public DepartmentDbContext(DbContextOptions<DepartmentDbContext> options) : base(options) { }

        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed data matching EmployeeService DepartmentIds
            modelBuilder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, Name = "Engineering",  Description = "Software development team",    Location = "Floor 3", IsActive = true, CreatedAt = new DateTime(2019,1,1) },
                new Department { DepartmentId = 2, Name = "Human Resources", Description = "HR and recruitment",      Location = "Floor 1", IsActive = true, CreatedAt = new DateTime(2019,1,1) },
                new Department { DepartmentId = 3, Name = "Finance",     Description = "Accounting and finance",       Location = "Floor 2", IsActive = true, CreatedAt = new DateTime(2019,1,1) },
                new Department { DepartmentId = 4, Name = "Marketing",   Description = "Marketing and communications", Location = "Floor 2", IsActive = true, CreatedAt = new DateTime(2019,1,1) }
            );
        }
    }
}
