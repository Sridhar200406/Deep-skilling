using Day6_Migrations.Models;
using Microsoft.EntityFrameworkCore;

namespace Day6_Migrations.Data
{
    public class AppDbContext : DbContext
    {
        // ── DbSets (tables) ──────────────────────────────────
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee>   Employees   { get; set; }

        // ── Configure SQLite database file ───────────────────
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=company.db");
        }

        // ── Fluent API configuration ─────────────────────────
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Department configuration
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
                entity.Property(d => d.Location).HasMaxLength(200);
            });

            // Employee configuration
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.JoinedDate).IsRequired();

                // Relationship: Employee belongs to one Department
                entity.HasOne(e => e.Dept)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── DATA SEEDING ─────────────────────────────────
            // Seed Departments
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "IT",      Location = "New York"    },
                new Department { Id = 2, Name = "HR",      Location = "Chicago"     },
                new Department { Id = 3, Name = "Finance", Location = "Los Angeles" }
            );

            // Seed Employees
            modelBuilder.Entity<Employee>().HasData(
                new Employee { Id = 1, Name = "John",  DepartmentId = 1, Salary = 50000, JoinedDate = new DateTime(2020, 1, 15) },
                new Employee { Id = 2, Name = "David", DepartmentId = 2, Salary = 35000, JoinedDate = new DateTime(2019, 3, 10) },
                new Employee { Id = 3, Name = "Mary",  DepartmentId = 1, Salary = 60000, JoinedDate = new DateTime(2021, 6, 1)  },
                new Employee { Id = 4, Name = "Smith", DepartmentId = 3, Salary = 45000, JoinedDate = new DateTime(2018, 9, 20) },
                new Employee { Id = 5, Name = "James", DepartmentId = 2, Salary = 40000, JoinedDate = new DateTime(2022, 2, 5)  }
            );
        }
    }
}
