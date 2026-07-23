using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EmployeeManagement.AzureFunctions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.AzureFunctions.Services;

public interface IBlobProcessingService
{
    Task<BlobMetadata> ExtractMetadataAsync(string blobName, string containerName, Stream blobStream);
    Task MoveToProcessedContainerAsync(string sourceBlobName, string sourceContainer, string destContainer);
    Task<List<string>> ListTempBlobsOlderThanAsync(string containerName, TimeSpan age);
    Task DeleteBlobAsync(string containerName, string blobName);
}

/// <summary>
/// Handles blob metadata extraction and file management for Azure Functions.
/// Detects file type, reads metadata tags, and can move blobs between containers.
/// </summary>
public class BlobProcessingService : IBlobProcessingService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobProcessingService> _logger;

    // Content type → category mapping
    private static readonly Dictionary<string, string> ContentTypeCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"]       = "Image",
        ["image/png"]        = "Image",
        ["image/gif"]        = "Image",
        ["image/webp"]       = "Image",
        ["application/pdf"]  = "Document",
        ["application/msword"] = "Document",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = "Document",
        ["application/vnd.ms-excel"] = "Spreadsheet",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = "Spreadsheet",
        ["text/plain"]       = "Text"
    };

    public BlobProcessingService(IConfiguration config, ILogger<BlobProcessingService> logger)
    {
        _logger = logger;
        var connectionString = config["AzureBlobStorage:ConnectionString"]
            ?? config["AzureWebJobsStorage"]
            ?? "UseDevelopmentStorage=true";

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<BlobMetadata> ExtractMetadataAsync(
        string blobName, string containerName, Stream blobStream)
    {
        _logger.LogInformation("Extracting metadata for blob '{BlobName}' in '{Container}'",
            blobName, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        BlobProperties? properties = null;
        IDictionary<string, string>? metadata = null;

        try
        {
            var propsResponse = await blobClient.GetPropertiesAsync();
            properties = propsResponse.Value;
            metadata = properties.Metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read blob properties for '{BlobName}'", blobName);
        }

        var contentType = properties?.ContentType ?? DetectContentTypeFromName(blobName);
        var fileSize = blobStream.CanSeek ? blobStream.Length : properties?.ContentLength ?? 0;

        var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var isDocument = ContentTypeCategories.TryGetValue(contentType, out var category)
            && (category == "Document" || category == "Spreadsheet" || category == "Text");

        // Try to extract EmployeeId from blob name: "employees/{id}/..."
        int? employeeId = null;
        var parts = blobName.Split('/');
        if (parts.Length >= 2 && int.TryParse(parts[1], out var parsed))
            employeeId = parsed;

        var originalFileName = metadata != null && metadata.TryGetValue("OriginalFileName", out var fn)
            ? fn
            : Path.GetFileName(blobName);

        var uploadedAt = metadata != null && metadata.TryGetValue("UploadedAt", out var ua)
            && DateTime.TryParse(ua, out var uaDate) ? uaDate : DateTime.UtcNow;

        var result = new BlobMetadata
        {
            BlobName = blobName,
            ContainerName = containerName,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            EmployeeId = employeeId,
            OriginalFileName = originalFileName,
            UploadedAt = uploadedAt,
            IsImage = isImage,
            IsDocument = isDocument,
            Status = BlobProcessingStatus.Processed,
            ProcessingNotes = $"Processed at {DateTime.UtcNow:O}. Category: {category ?? "Unknown"}"
        };

        _logger.LogInformation(
            "Blob '{BlobName}': Size={Size}, Type={ContentType}, EmployeeId={EmployeeId}, IsImage={IsImage}",
            blobName, result.FileSizeDisplay, contentType, employeeId, isImage);

        return result;
    }

    public async Task MoveToProcessedContainerAsync(
        string sourceBlobName, string sourceContainer, string destContainer)
    {
        var sourceContainerClient = _blobServiceClient.GetBlobContainerClient(sourceContainer);
        var destContainerClient   = _blobServiceClient.GetBlobContainerClient(destContainer);

        // Ensure destination container exists
        await destContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var sourceBlobClient = sourceContainerClient.GetBlobClient(sourceBlobName);
        var destBlobName = $"processed/{DateTime.UtcNow:yyyy/MM/dd}/{sourceBlobName}";
        var destBlobClient = destContainerClient.GetBlobClient(destBlobName);

        // Copy then delete source
        var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
        await copyOperation.WaitForCompletionAsync();

        _logger.LogInformation("Copied blob '{Source}' → '{Dest}'", sourceBlobName, destBlobName);
    }

    public async Task<List<string>> ListTempBlobsOlderThanAsync(string containerName, TimeSpan age)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        if (!await containerClient.ExistsAsync()) return new List<string>();

        var cutoff = DateTime.UtcNow - age;
        var oldBlobs = new List<string>();

        await foreach (var blob in containerClient.GetBlobsAsync(prefix: "temp/"))
        {
            if (blob.Properties.LastModified.HasValue
                && blob.Properties.LastModified.Value.UtcDateTime < cutoff)
            {
                oldBlobs.Add(blob.Name);
            }
        }

        _logger.LogInformation("Found {Count} temp blobs older than {Age} in '{Container}'",
            oldBlobs.Count, age, containerName);

        return oldBlobs;
    }

    public async Task DeleteBlobAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var deleted = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

        if (deleted.Value)
            _logger.LogInformation("Deleted blob '{BlobName}' from '{Container}'", blobName, containerName);
    }

    private static string DetectContentTypeFromName(string blobName)
    {
        var ext = Path.GetExtension(blobName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".pdf"            => "application/pdf",
            ".doc"            => "application/msword",
            ".docx"           => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls"            => "application/vnd.ms-excel",
            ".xlsx"           => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt"            => "text/plain",
            _                 => "application/octet-stream"
        };
    }
}
