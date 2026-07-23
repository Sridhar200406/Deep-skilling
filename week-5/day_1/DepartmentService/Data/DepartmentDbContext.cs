using Microsoft.EntityFrameworkCore;
using DepartmentService.Models;

namespace DepartmentService.Data
{
    public class DepartmentDbContext : DbContext
    {
        public DepartmentDbContext(DbContextOptions<DepartmentDbContext> options) : base(options) { }

        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, DepartmentName = "Engineering", Description = "Software development and architecture" },
                new Department { DepartmentId = 2, DepartmentName = "Human Resources", Description = "Recruitment and employee welfare" },
                new Department { DepartmentId = 3, DepartmentName = "Finance", Description = "Accounting and financial planning" },
                new Department { DepartmentId = 4, DepartmentName = "Marketing", Description = "Brand and product promotion" }
            );
        }
    }
}
