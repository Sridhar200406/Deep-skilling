using DepartmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DepartmentService.Data;

public class DepartmentDbContext : DbContext
{
    public DepartmentDbContext(DbContextOptions<DepartmentDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(d => d.Name).IsUnique();
            e.Property(d => d.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "Engineering",     Description = "Software Engineering", IsActive = true, CreatedAt = new DateTime(2024,1,1) },
            new Department { Id = 2, Name = "Human Resources", Description = "HR Department",        IsActive = true, CreatedAt = new DateTime(2024,1,1) },
            new Department { Id = 3, Name = "Finance",         Description = "Finance Department",   IsActive = true, CreatedAt = new DateTime(2024,1,1) },
            new Department { Id = 4, Name = "Marketing",       Description = "Marketing Department", IsActive = true, CreatedAt = new DateTime(2024,1,1) }
        );
    }
}
