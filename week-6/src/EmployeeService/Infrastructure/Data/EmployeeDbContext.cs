using EmployeeService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Infrastructure.Data;

public class EmployeeDbContext : DbContext
{
    public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);

            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Employees)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Department configuration
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(d => d.Name).IsUnique();
            entity.Property(d => d.Description).HasMaxLength(500);
        });

        // EmployeeDocument configuration
        modelBuilder.Entity<EmployeeDocument>(entity =>
        {
            entity.HasKey(ed => ed.Id);
            entity.Property(ed => ed.FileName).IsRequired().HasMaxLength(255);
            entity.Property(ed => ed.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(ed => ed.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(ed => ed.BlobUrl).IsRequired().HasMaxLength(1000);
            entity.Property(ed => ed.BlobName).IsRequired().HasMaxLength(500);

            entity.HasOne(ed => ed.Employee)
                  .WithMany(e => e.Documents)
                  .HasForeignKey(ed => ed.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationUser configuration
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
        });

        // Seed data
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "Engineering", Description = "Software Engineering Department", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
            new Department { Id = 2, Name = "Human Resources", Description = "HR Department", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
            new Department { Id = 3, Name = "Finance", Description = "Finance Department", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
            new Department { Id = 4, Name = "Marketing", Description = "Marketing Department", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
        );

        // Seed admin user (password: Admin@123)
        modelBuilder.Entity<ApplicationUser>().HasData(
            new ApplicationUser
            {
                Id = 1,
                Username = "admin",
                Email = "admin@company.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1)
            }
        );
    }
}
