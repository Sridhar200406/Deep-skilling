using System.Text;
using System.Text.Json;
using EmployeeManagement.AzureFunctions.Models;
using EmployeeManagement.AzureFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.AzureFunctions.Functions;

/// <summary>
/// TIMER TRIGGER FUNCTION
/// ─────────────────────────────────────────────────────────────────────────────
/// What is a Timer Trigger?
///   A Timer Trigger invokes a Function on a defined schedule using a cron
///   expression. It's like Task Scheduler / cron jobs but serverless.
///   No server needs to be running — Azure wakes up the function at the
///   scheduled time, runs it, then the compute goes back to zero.
///
/// Schedule: "0 0 6 * * *"  →  Every day at 06:00 UTC
///   Cron format: {second} {minute} {hour} {day} {month} {dayOfWeek}
///   Configurable via "Cleanup:Schedule" app setting.
///
/// What it does (3 tasks in one run):
///   1. DAILY REPORT   — Queries Azure SQL, generates stats, emails admin
///   2. INACTIVE SCAN  — Finds employees with no activity > N days
///   3. TEMP CLEANUP   — Deletes temporary blobs older than 24h
///
/// Flow:
///   Azure timer fires at 06:00 UTC
///     ↓
///   Task 1: Query DB → generate DailyReportSummary → email admin
///     ↓
///   Task 2: Query DB → find inactive employees → log/notify
///     ↓
///   Task 3: List temp/* blobs → delete those older than threshold
///     ↓
///   Log execution summary to Application Insights
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class EmployeeCleanupFunction
{
    private readonly IEmployeeReportService _reportService;
    private readonly IBlobProcessingService _blobService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _config;
    private readonly ILogger<EmployeeCleanupFunction> _logger;

    public EmployeeCleanupFunction(
        IEmployeeReportService reportService,
        IBlobProcessingService blobService,
        INotificationService notificationService,
        IConfiguration config,
        ILogger<EmployeeCleanupFunction> logger)
    {
        _reportService = reportService;
        _blobService = blobService;
        _notificationService = notificationService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Timer schedule is loaded from configuration so it can be changed without redeployment.
    /// Default: "0 0 6 * * *" = every day at 06:00 UTC
    /// For testing:  "0 */5 * * * *" = every 5 minutes
    /// </summary>
    [Function("EmployeeCleanup")]
    public async Task Run(
        [TimerTrigger("%Cleanup:Schedule%")] TimerInfo timerInfo)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "EmployeeCleanup timer triggered at {Time}. IsPastDue: {IsPastDue}",
            startTime, timerInfo.IsPastDue);

        if (timerInfo.IsPastDue)
        {
            _logger.LogWarning(
                "Timer is running late (past due). This may indicate the Function App was restarted.");
        }

        var overallSuccess = true;
        var taskResults = new List<string>();

        // ── Task 1: Generate Daily Report ─────────────────────────────────
        try
        {
            _logger.LogInformation("Task 1/3: Generating daily employee report...");
            var report = await _reportService.GenerateDailyReportAsync();
            var reportHtml = BuildReportHtml(report);

            var adminEmail = _config["Report:AdminEmail"] ?? "admin@company.com";
            var subject = $"Daily Employee Report — {report.ReportDate:yyyy-MM-dd}";

            await _notificationService.SendAdminReportAsync(subject, reportHtml, adminEmail);

            var result = $"✓ Daily report: {report.TotalActiveEmployees} active employees, " +
                         $"{report.NewHiresThisMonth} new hires, {report.DocumentsUploaded} docs uploaded";
            taskResults.Add(result);
            _logger.LogInformation("Task 1 complete: {Result}", result);
        }
        catch (Exception ex)
        {
            overallSuccess = false;
            var msg = $"✗ Daily report FAILED: {ex.Message}";
            taskResults.Add(msg);
            _logger.LogError(ex, "Task 1 FAILED: Daily report generation error");
        }

        // ── Task 2: Inactive Employee Scan ────────────────────────────────
        try
        {
            _logger.LogInformation("Task 2/3: Scanning for inactive employees...");

            var threshold = int.Parse(_config["Cleanup:InactiveDaysThreshold"] ?? "90");
            var cleanup = await _reportService.MarkStaleRecordsAsync(threshold);

            var result = $"✓ Inactive scan: {cleanup.InactiveEmployeesFound} employees " +
                         $"inactive for >{threshold} days identified";
            taskResults.Add(result);

            if (cleanup.InactiveEmployeesFound > 0)
            {
                foreach (var item in cleanup.ProcessedItems.Take(10)) // log first 10
                    _logger.LogInformation("  {Item}", item);

                if (cleanup.ProcessedItems.Count > 10)
                    _logger.LogInformation("  ... and {More} more", cleanup.ProcessedItems.Count - 10);
            }

            _logger.LogInformation("Task 2 complete: {Result}", result);
        }
        catch (Exception ex)
        {
            overallSuccess = false;
            var msg = $"✗ Inactive scan FAILED: {ex.Message}";
            taskResults.Add(msg);
            _logger.LogError(ex, "Task 2 FAILED: Inactive employee scan error");
        }

        // ── Task 3: Temp Blob Cleanup ─────────────────────────────────────
        try
        {
            _logger.LogInformation("Task 3/3: Cleaning up temporary blobs...");

            var tempAgeHours = int.Parse(_config["Cleanup:TempFileAgeHours"] ?? "24");
            var containerName = _config["AzureBlobStorage:ContainerName"] ?? "employee-files";
            var oldBlobs = await _blobService.ListTempBlobsOlderThanAsync(
                containerName, TimeSpan.FromHours(tempAgeHours));

            long bytesFreed = 0;
            foreach (var blobName in oldBlobs)
            {
                await _blobService.DeleteBlobAsync(containerName, blobName);
                _logger.LogInformation("Deleted temp blob: '{BlobName}'", blobName);
            }

            var result = $"✓ Temp cleanup: {oldBlobs.Count} blob(s) deleted";
            taskResults.Add(result);
            _logger.LogInformation("Task 3 complete: {Result}", result);
        }
        catch (Exception ex)
        {
            overallSuccess = false;
            var msg = $"✗ Temp cleanup FAILED: {ex.Message}";
            taskResults.Add(msg);
            _logger.LogError(ex, "Task 3 FAILED: Temp blob cleanup error");
        }

        // ── Final Summary ─────────────────────────────────────────────────
        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        _logger.LogInformation(
            "EmployeeCleanup completed in {Elapsed:F1}s. Status: {Status}. Results:\n{Results}",
            elapsed,
            overallSuccess ? "SUCCESS" : "PARTIAL FAILURE",
            string.Join("\n", taskResults));

        if (timerInfo.ScheduleStatus != null)
        {
            _logger.LogInformation(
                "Next scheduled run: {NextRun}",
                timerInfo.ScheduleStatus.Next);
        }
    }

    // ── HTML Report Builder ───────────────────────────────────────────────────

    private static string BuildReportHtml(DailyReportSummary report)
    {
        var deptRows = new StringBuilder();
        foreach (var dept in report.DepartmentBreakdown)
        {
            deptRows.AppendLine($"""
                <tr>
                  <td style="padding:8px; border-bottom:1px solid #dee2e6;">{dept.DepartmentName}</td>
                  <td style="padding:8px; border-bottom:1px solid #dee2e6; text-align:center;">{dept.EmployeeCount}</td>
                </tr>
                """);
        }

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 700px; margin: 0 auto; padding: 20px;">
              <div style="background:#107c10; padding:20px; border-radius:8px 8px 0 0;">
                <h1 style="color:white; margin:0; font-size:22px;">Daily Employee Report</h1>
                <p style="color:#e8f5e9; margin:5px 0 0;">{report.ReportDate:dddd, MMMM d, yyyy} UTC</p>
              </div>
              <div style="background:#f8f9fa; padding:30px; border:1px solid #dee2e6; border-top:none;">
                <h2 style="color:#212529; font-size:18px;">Summary</h2>
                <table style="width:100%; border-collapse:collapse;">
                  <tr style="background:#e8f5e9;">
                    <td style="padding:12px; font-weight:bold;">Active Employees</td>
                    <td style="padding:12px; font-size:20px; color:#107c10;"><strong>{report.TotalActiveEmployees}</strong></td>
                  </tr>
                  <tr>
                    <td style="padding:12px; font-weight:bold;">Departments</td>
                    <td style="padding:12px;">{report.TotalDepartments}</td>
                  </tr>
                  <tr style="background:#e8f5e9;">
                    <td style="padding:12px; font-weight:bold;">New Hires (This Month)</td>
                    <td style="padding:12px;">{report.NewHiresThisMonth}</td>
                  </tr>
                  <tr>
                    <td style="padding:12px; font-weight:bold;">Inactive Employees</td>
                    <td style="padding:12px; color:{(report.InactiveEmployees > 0 ? "#d83b01" : "#107c10")};">
                      {report.InactiveEmployees}
                    </td>
                  </tr>
                  <tr style="background:#e8f5e9;">
                    <td style="padding:12px; font-weight:bold;">Documents Uploaded (24h)</td>
                    <td style="padding:12px;">{report.DocumentsUploaded}</td>
                  </tr>
                </table>

                <h2 style="color:#212529; font-size:18px; margin-top:25px;">Department Breakdown</h2>
                <table style="width:100%; border-collapse:collapse;">
                  <thead>
                    <tr style="background:#0078d4; color:white;">
                      <th style="padding:10px; text-align:left;">Department</th>
                      <th style="padding:10px; text-align:center;">Employees</th>
                    </tr>
                  </thead>
                  <tbody>
                    {deptRows}
                  </tbody>
                </table>

                <hr style="border:none; border-top:1px solid #dee2e6; margin:25px 0;" />
                <p style="color:#6c757d; font-size:12px;">
                  Generated by Employee Management Azure Functions — {report.ReportDate:O}
                </p>
              </div>
            </body>
            </html>
            """;
    }
}
