using EmployeeManagement.AzureFunctions.Data;
using EmployeeManagement.AzureFunctions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.AzureFunctions.Services;

public interface IEmployeeReportService
{
    Task<DailyReportSummary> GenerateDailyReportAsync();
    Task<List<InactiveEmployeeRecord>> GetInactiveEmployeesAsync(int inactiveDaysThreshold);
    Task<CleanupResult> MarkStaleRecordsAsync(int inactiveDaysThreshold);
}

/// <summary>
/// Queries Azure SQL via EF Core to generate reports and identify cleanup candidates.
/// Used by the Timer Trigger function.
/// </summary>
public class EmployeeReportService : IEmployeeReportService
{
    private readonly FunctionDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<EmployeeReportService> _logger;

    public EmployeeReportService(
        FunctionDbContext db,
        IConfiguration config,
        ILogger<EmployeeReportService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<DailyReportSummary> GenerateDailyReportAsync()
    {
        _logger.LogInformation("Generating daily employee report...");

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalActive      = await _db.Employees.CountAsync(e => e.IsActive);
        var totalDepartments = await _db.Departments.CountAsync(d => d.IsActive);
        var newHires         = await _db.Employees.CountAsync(e => e.HireDate >= startOfMonth && e.IsActive);
        var inactive         = await _db.Employees.CountAsync(e => !e.IsActive);
        var documents        = await _db.EmployeeDocuments.CountAsync(d =>
            d.UploadedAt >= now.AddDays(-1) && !d.IsDeleted);

        var deptBreakdown = await _db.Employees
            .Where(e => e.IsActive)
            .Join(_db.Departments, e => e.DepartmentId, d => d.Id,
                (e, d) => new { d.Name })
            .GroupBy(x => x.Name)
            .Select(g => new DepartmentSummary
            {
                DepartmentName = g.Key,
                EmployeeCount  = g.Count()
            })
            .OrderByDescending(d => d.EmployeeCount)
            .ToListAsync();

        var summary = new DailyReportSummary
        {
            ReportDate          = now,
            TotalActiveEmployees = totalActive,
            TotalDepartments    = totalDepartments,
            NewHiresThisMonth   = newHires,
            InactiveEmployees   = inactive,
            DocumentsUploaded   = documents,
            DepartmentBreakdown = deptBreakdown
        };

        _logger.LogInformation(
            "Report: Active={Active}, Departments={Depts}, NewHires={New}, Inactive={Inactive}",
            totalActive, totalDepartments, newHires, inactive);

        return summary;
    }

    public async Task<List<InactiveEmployeeRecord>> GetInactiveEmployeesAsync(int inactiveDaysThreshold)
    {
        var cutoff = DateTime.UtcNow.AddDays(-inactiveDaysThreshold);

        var inactive = await _db.Employees
            .Where(e => e.IsActive && e.UpdatedAt.HasValue && e.UpdatedAt < cutoff)
            .Join(_db.Departments, e => e.DepartmentId, d => d.Id,
                (e, d) => new InactiveEmployeeRecord
                {
                    EmployeeId   = e.Id,
                    FullName     = $"{e.FirstName} {e.LastName}",
                    Email        = e.Email,
                    Department   = d.Name,
                    LastUpdated  = e.UpdatedAt,
                    DaysInactive = (int)(DateTime.UtcNow - e.UpdatedAt!.Value).TotalDays
                })
            .OrderByDescending(e => e.DaysInactive)
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} employees inactive for more than {Days} days",
            inactive.Count, inactiveDaysThreshold);

        return inactive;
    }

    public async Task<CleanupResult> MarkStaleRecordsAsync(int inactiveDaysThreshold)
    {
        var result = new CleanupResult();
        var inactive = await GetInactiveEmployeesAsync(inactiveDaysThreshold);
        result.InactiveEmployeesFound = inactive.Count;

        foreach (var emp in inactive)
        {
            result.ProcessedItems.Add(
                $"[INACTIVE] Employee {emp.EmployeeId}: {emp.FullName} — {emp.DaysInactive} days inactive");
        }

        // NOTE: In production, this would send notifications or mark records.
        // We log only here to avoid accidental bulk updates in background jobs.
        // Actual deactivation should be an explicit admin action.
        _logger.LogInformation(
            "Cleanup scan complete. {InactiveCount} inactive employees identified.",
            result.InactiveEmployeesFound);

        return result;
    }
}
