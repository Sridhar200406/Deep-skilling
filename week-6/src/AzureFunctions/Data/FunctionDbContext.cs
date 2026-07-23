using EmployeeManagement.AzureFunctions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EmployeeManagement.AzureFunctions.Data;

/// <summary>
/// Lightweight EF Core context for Azure Functions.
/// Only maps the tables Functions need — does NOT duplicate the full Employee API context.
/// Read-only projections avoid accidental writes from background functions.
/// </summary>
public class FunctionDbContext : DbContext
{
    public FunctionDbContext(DbContextOptions<FunctionDbContext> options) : base(options) { }

    // Raw sets — used for cleanup/reporting queries
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<EmployeeDocumentEntity> EmployeeDocuments => Set<EmployeeDocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeEntity>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<DepartmentEntity>(e =>
        {
            e.ToTable("Departments");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<EmployeeDocumentEntity>(e =>
        {
            e.ToTable("EmployeeDocuments");
            e.HasKey(x => x.Id);
        });
    }
}

// ─── Lightweight Entity classes (mirrors main API entities — no navigation duplication) ──

public class EmployeeEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DepartmentId { get; set; }
    public decimal Salary { get; set; }
}

public class DepartmentEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EmployeeDocumentEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
}
