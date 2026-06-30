using Microsoft.EntityFrameworkCore;
using Week2_Day4_EFCoreCRUD.Models;

namespace Week2_Day4_EFCoreCRUD.Data
{
    /// <summary>
    /// AppDbContext — the EF Core gateway to the SQL Server database.
    /// Unchanged from Day 2. Only extended here if required.
    /// </summary>
    public class AppDbContext : DbContext
    {
        // Represents the Employees table in the database
        public DbSet<Employee> Employees { get; set; }

        /// <summary>
        /// Configures the SQL Server connection string.
        /// Uses the same EFCoreEmployeeDB database created in Day 2.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(
                "Server=.;Database=EFCoreEmployeeDB;Trusted_Connection=True;TrustServerCertificate=True");
        }

        /// <summary>
        /// Fluent API configuration for the Employee entity.
        /// Defines column types, constraints, and table mapping.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Department)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.Salary)
                      .HasColumnType("decimal(18,2)");
            });
        }
    }
}
