using EmployeeService.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Data
{
    public class EmployeeDbContext : DbContext
    {
        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options) { }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Employee>().HasData(
                new Employee { EmployeeId=1, FirstName="Alice",  LastName="Johnson",  Email="alice@company.com",  Phone="555-0101", Position="Senior Developer",  Salary=95000m, HireDate=new DateTime(2020,3,15), IsActive=true, DepartmentId=1 },
                new Employee { EmployeeId=2, FirstName="Bob",    LastName="Smith",    Email="bob@company.com",    Phone="555-0102", Position="HR Manager",        Salary=75000m, HireDate=new DateTime(2019,6,1),  IsActive=true, DepartmentId=2 },
                new Employee { EmployeeId=3, FirstName="Carol",  LastName="Williams", Email="carol@company.com",  Phone="555-0103", Position="Financial Analyst", Salary=80000m, HireDate=new DateTime(2021,1,10), IsActive=true, DepartmentId=3 }
            );
        }
    }
}
