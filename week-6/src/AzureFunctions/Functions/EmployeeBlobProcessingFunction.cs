using EmployeeManagement.AzureFunctions.Models;
using EmployeeManagement.AzureFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.AzureFunctions.Functions;

/// <summary>
/// BLOB TRIGGER FUNCTION
/// ─────────────────────────────────────────────────────────────────────────────
/// What is a Blob Trigger?
///   A Blob Trigger fires automatically whenever a new blob (file) is created
///   or updated in a specified Azure Blob Storage container.
///   No polling needed — Azure Event Grid or storage webhooks detect the change
///   and invoke this function within seconds.
///
/// Trigger container: "employee-files/{name}"
///   Watches the same container the Employee API uploads files to.
///   {name} is bound to the blob's full name including any path prefix.
///
/// What it does:
///   1. Detects that a new employee document/image was uploaded
///   2. Reads the blob stream and extracts metadata
///   3. Logs file details to Application Insights
///   4. Identifies if it's an image (profile photo) vs document
///   5. Tags the blob with processing status metadata
///   6. Sends a confirmation notification to the employee
///
/// Flow:
///   Employee API uploads file to Blob Storage
///     ↓
///   Azure detects new blob in "employee-files" container
///     ↓
///   This function is invoked automatically with the blob stream
///     ↓
///   Extract metadata, log, process
///     ↓
///   Optionally send notification
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class EmployeeBlobProcessingFunction
{
    private readonly IBlobProcessingService _blobService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _config;
    private readonly ILogger<EmployeeBlobProcessingFunction> _logger;

    public EmployeeBlobProcessingFunction(
        IBlobProcessingService blobService,
        INotificationService notificationService,
        IConfiguration config,
        ILogger<EmployeeBlobProcessingFunction> logger)
    {
        _blobService = blobService;
        _notificationService = notificationService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Fires when any blob is created/updated in the "employee-files" container.
    /// The {name} parameter captures the full blob path (e.g. "employees/1/abc_resume.pdf").
    /// </summary>
    [Function("EmployeeBlobProcessing")]
    public async Task Run(
        [BlobTrigger("employee-files/{name}", Connection = "AzureBlobStorage:ConnectionString")]
        Stream blobStream,
        string name,
        FunctionContext context)
    {
        var containerName = "employee-files";

        _logger.LogInformation(
            "Blob trigger fired. Blob: '{BlobName}' | Container: '{Container}' | Size: {Size} bytes",
            name, containerName, blobStream.Length);

        try
        {
            // ── Step 1: Extract metadata ──────────────────────────────────
            var metadata = await _blobService.ExtractMetadataAsync(name, containerName, blobStream);

            // ── Step 2: Log structured details to Application Insights ────
            LogBlobDetails(metadata);

            // ── Step 3: Validate file type (security check) ───────────────
            if (IsSuspiciousFile(name, metadata.ContentType))
            {
                _logger.LogWarning(
                    "SECURITY: Suspicious file detected — Blob: '{BlobName}', ContentType: '{ContentType}'. " +
                    "Consider blocking this file type.",
                    name, metadata.ContentType);
                // In production: move to quarantine container, alert security team
                return;
            }

            // ── Step 4: Route based on file type ──────────────────────────
            if (metadata.IsImage)
            {
                _logger.LogInformation(
                    "Image detected for Employee {EmployeeId}. File: '{FileName}' ({Size})",
                    metadata.EmployeeId, metadata.OriginalFileName, metadata.FileSizeDisplay);
                // In production: generate thumbnails, resize for profile picture
            }
            else if (metadata.IsDocument)
            {
                _logger.LogInformation(
                    "Document detected for Employee {EmployeeId}. File: '{FileName}' ({Size})",
                    metadata.EmployeeId, metadata.OriginalFileName, metadata.FileSizeDisplay);
                // In production: extract text for search indexing, virus scan
            }

            // ── Step 5: Send upload confirmation notification ─────────────
            if (metadata.EmployeeId.HasValue)
            {
                await SendUploadNotificationAsync(metadata);
            }

            _logger.LogInformation(
                "Blob processing complete for '{BlobName}'. Status: {Status}",
                name, metadata.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing blob '{BlobName}' in container '{Container}'",
                name, containerName);
            throw; // Re-throw so Azure retries (up to 5 times by default)
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void LogBlobDetails(BlobMetadata metadata)
    {
        _logger.LogInformation(
            "Blob details — Name: '{BlobName}' | Size: {Size} | ContentType: {ContentType} | " +
            "EmployeeId: {EmployeeId} | OriginalFile: '{OriginalFile}' | " +
            "IsImage: {IsImage} | IsDocument: {IsDocument} | UploadedAt: {UploadedAt}",
            metadata.BlobName,
            metadata.FileSizeDisplay,
            metadata.ContentType,
            metadata.EmployeeId?.ToString() ?? "unknown",
            metadata.OriginalFileName,
            metadata.IsImage,
            metadata.IsDocument,
            metadata.UploadedAt);
    }

    private static bool IsSuspiciousFile(string blobName, string contentType)
    {
        var suspiciousExtensions = new[] { ".exe", ".bat", ".sh", ".ps1", ".cmd", ".dll", ".js", ".vbs" };
        var ext = Path.GetExtension(blobName).ToLowerInvariant();
        return suspiciousExtensions.Contains(ext)
            || contentType.Contains("executable", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SendUploadNotificationAsync(BlobMetadata metadata)
    {
        try
        {
            // We don't have the employee email in the blob trigger context,
            // so in a real scenario you'd query the DB or use a message queue.
            // Here we just log the intent — the notification is handled by the HTTP trigger.
            _logger.LogInformation(
                "Upload confirmation logged for Employee {EmployeeId}. " +
                "File '{OriginalFile}' ({Size}) is ready.",
                metadata.EmployeeId, metadata.OriginalFileName, metadata.FileSizeDisplay);
        }
        catch (Exception ex)
        {
            // Notification failure should not fail the blob processing
            _logger.LogWarning(ex, "Could not send upload notification for Employee {EmployeeId}",
                metadata.EmployeeId);
        }
    }
}
