using Microsoft.EntityFrameworkCore;
using Week2_Day7_LoadingStrategies.Models;

namespace Week2_Day7_LoadingStrategies.Data
{
    /// <summary>
    /// AppDbContext — Day 7 extension.
    ///
    /// Changes from Day 6:
    ///   1. UseLazyLoadingProxies() added in OnConfiguring → enables Lazy Loading.
    ///   2. Same SQLite DB, same seed data, same relationships.
    ///
    /// Loading strategies configured here:
    ///   - Eager Loading  : .Include() called at query time (no change to context)
    ///   - Lazy Loading   : UseLazyLoadingProxies() + virtual navigation properties
    ///   - Explicit Loading: db.Entry(entity).Reference/Collection.Load() at runtime
    ///   - AsNoTracking   : .AsNoTracking() on read-only queries (no change to context)
    /// </summary>
    public class AppDbContext : DbContext
    {
        // ── DbSets (tables) ───────────────────────────────────────────
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee>   Employees   { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options
                // Same SQLite file as Day 6
                .UseSqlite("Data Source=company_day7.db")
                // DAY 7: Enable Lazy Loading via Castle DynamicProxy.
                // EF Core generates proxy classes that wrap navigation
                // properties. When accessed, the proxy fires a new SQL
                // query automatically to load the related data.
                // REQUIREMENT: navigation properties must be virtual.
                .UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── Department configuration (unchanged from Day 6) ───────
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
                entity.Property(d => d.Location).HasMaxLength(200);
            });

            // ── Employee configuration (unchanged from Day 6) ─────────
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Department).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.JoinedDate).IsRequired();

                // One-to-Many: Employee → Department
                entity.HasOne(e => e.Dept)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Data Seeding (same as Day 6) ──────────────────────────
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "IT",      Location = "New York"    },
                new Department { Id = 2, Name = "HR",      Location = "Chicago"     },
                new Department { Id = 3, Name = "Finance", Location = "Los Angeles" }
            );

            modelBuilder.Entity<Employee>().HasData(
                new Employee { EmployeeId = 1, Name = "John",  Department = "IT",      DepartmentId = 1, Salary = 50000, JoinedDate = new DateTime(2020, 1, 15) },
                new Employee { EmployeeId = 2, Name = "David", Department = "HR",      DepartmentId = 2, Salary = 35000, JoinedDate = new DateTime(2019, 3, 10) },
                new Employee { EmployeeId = 3, Name = "Mary",  Department = "IT",      DepartmentId = 1, Salary = 60000, JoinedDate = new DateTime(2021, 6, 1)  },
                new Employee { EmployeeId = 4, Name = "Smith", Department = "Finance", DepartmentId = 3, Salary = 45000, JoinedDate = new DateTime(2018, 9, 20) },
                new Employee { EmployeeId = 5, Name = "James", Department = "HR",      DepartmentId = 2, Salary = 40000, JoinedDate = new DateTime(2022, 2, 5)  },
                new Employee { EmployeeId = 6, Name = "Alice", Department = "IT",      DepartmentId = 1, Salary = 55000, JoinedDate = new DateTime(2023, 7, 1)  }
            );
        }
    }
}
