using Microsoft.EntityFrameworkCore;
using Week2_Day2_EFCoreCRUD.Models;

namespace Week2_Day2_EFCoreCRUD.Data
{
    /// <summary>
    /// The EF Core database context for this application.
    /// Acts as the bridge between C# model classes and the SQL Server database.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Represents the Employees table in the database.
        /// Used to query and save Employee instances.
        /// </summary>
        public DbSet<Employee> Employees { get; set; }

        /// <summary>
        /// Configures the database connection.
        /// Using SQL Server with a local instance and Windows Authentication.
        /// </summary>
        /// <param name="optionsBuilder">Builder used to configure EF Core options.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=.;Database=EFCoreEmployeeDB;Trusted_Connection=True;TrustServerCertificate=True");
        }

        /// <summary>
        /// Optional: configure model-level rules such as column constraints.
        /// </summary>
        /// <param name="modelBuilder">Builder used to shape the EF Core model.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set max length and required constraints for Employee fields
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
