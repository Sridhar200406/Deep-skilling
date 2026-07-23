using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.AzureFunctions.Models;

// ─── Request Models ──────────────────────────────────────────────────────────

/// <summary>
/// Payload received by the HTTP Trigger function from the Employee API.
/// Sent when an employee is created or updated.
/// </summary>
public class EmployeeNotificationRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    [Required]
    public NotificationEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? AdditionalMessage { get; set; }
}

public enum NotificationEventType
{
    EmployeeCreated,
    EmployeeUpdated,
    EmployeeDeactivated,
    DocumentUploaded,
    PasswordReset
}

// ─── Response Models ─────────────────────────────────────────────────────────

public class FunctionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string FunctionName { get; set; } = string.Empty;

    public static FunctionResponse Ok(string message, object? data = null, string function = "")
        => new() { Success = true, Message = message, Data = data, FunctionName = function };

    public static FunctionResponse Fail(string message, string function = "")
        => new() { Success = false, Message = message, FunctionName = function };
}

// ─── Blob Metadata Models ─────────────────────────────────────────────────────

public class BlobMetadata
{
    public string BlobName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeDisplay => FileSizeBytes < 1024 * 1024
        ? $"{FileSizeBytes / 1024.0:F1} KB"
        : $"{FileSizeBytes / (1024.0 * 1024):F2} MB";
    public int? EmployeeId { get; set; }
    public string? OriginalFileName { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsImage { get; set; }
    public bool IsDocument { get; set; }
    public BlobProcessingStatus Status { get; set; }
    public string? ProcessingNotes { get; set; }
}

public enum BlobProcessingStatus
{
    Detected,
    Processing,
    Processed,
    Failed
}

// ─── Cleanup / Report Models ──────────────────────────────────────────────────

public class InactiveEmployeeRecord
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public int DaysInactive { get; set; }
}

public class DailyReportSummary
{
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    public int TotalActiveEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int NewHiresThisMonth { get; set; }
    public int InactiveEmployees { get; set; }
    public int DocumentsUploaded { get; set; }
    public List<DepartmentSummary> DepartmentBreakdown { get; set; } = new();
}

public class DepartmentSummary
{
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
}

public class CleanupResult
{
    public int InactiveEmployeesFound { get; set; }
    public int TempFilesDeleted { get; set; }
    public long BytesFreed { get; set; }
    public List<string> ProcessedItems { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

// ─── DB Query Models (lightweight, no EF navigation) ─────────────────────────

public class EmployeeRecord
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
    public string DepartmentName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
