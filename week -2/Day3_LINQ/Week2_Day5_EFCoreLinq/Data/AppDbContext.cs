using Microsoft.EntityFrameworkCore;
using Week2_Day5_EFCoreLinq.Models;

namespace Week2_Day5_EFCoreLinq.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Same SQL Server connection as Day 2
            options.UseSqlServer(
                "Server=.;Database=EFCoreEmployeeDB;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Department).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
            });
        }
    }
}
