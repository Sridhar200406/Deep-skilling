using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeService.Infrastructure.Azure;

public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType, int employeeId);
    Task<BlobDownloadResult?> DownloadFileAsync(string blobName);
    Task<bool> DeleteFileAsync(string blobName);
    Task<bool> FileExistsAsync(string blobName);
}

public class BlobUploadResult
{
    public string BlobName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public class BlobDownloadResult
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _containerName;

    // Allowed file types for security
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "employee-files";

        var connectionString = configuration["AzureBlobStorage:ConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                "Azure Blob Storage connection string is not configured. " +
                "Set AzureBlobStorage:ConnectionString in Azure Key Vault or app settings.");

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
    }

    /// <summary>
    /// Ensures the blob container exists, creating it if necessary.
    /// Call this during app startup.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
        _logger.LogInformation("Azure Blob container '{ContainerName}' is ready.", _containerName);
    }

    public async Task<BlobUploadResult> UploadFileAsync(
        Stream fileStream, string fileName, string contentType, int employeeId)
    {
        // Validate content type
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed.");

        // Validate file size
        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"File size exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB.");

        // Generate a unique blob name to prevent overwrites and path traversal
        var extension = Path.GetExtension(fileName);
        var safeName = Path.GetFileNameWithoutExtension(fileName)
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "");

        var blobName = $"employees/{employeeId}/{Guid.NewGuid():N}_{safeName}{extension}";

        _logger.LogInformation("Uploading file '{FileName}' as blob '{BlobName}' for employee {EmployeeId}",
            fileName, blobName, employeeId);

        var blobClient = _containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            },
            Metadata = new Dictionary<string, string>
            {
                { "EmployeeId", employeeId.ToString() },
                { "OriginalFileName", fileName },
                { "UploadedAt", DateTime.UtcNow.ToString("O") }
            }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions);

        _logger.LogInformation("Successfully uploaded blob '{BlobName}'", blobName);

        return new BlobUploadResult
        {
            BlobName = blobName,
            BlobUrl = blobClient.Uri.ToString(),
            ContentType = contentType,
            FileSize = fileStream.Length
        };
    }

    public async Task<BlobDownloadResult?> DownloadFileAsync(string blobName)
    {
        // Sanitize blob name to prevent path traversal
        if (blobName.Contains("..") || blobName.StartsWith("/"))
        {
            _logger.LogWarning("Invalid blob name attempted: {BlobName}", blobName);
            return null;
        }

        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Blob '{BlobName}' not found.", blobName);
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync();
        var properties = await blobClient.GetPropertiesAsync();

        var originalFileName = properties.Value.Metadata.TryGetValue("OriginalFileName", out var fn)
            ? fn
            : Path.GetFileName(blobName);

        return new BlobDownloadResult
        {
            Content = response.Value.Content,
            ContentType = response.Value.Details.ContentType,
            FileName = originalFileName
        };
    }

    public async Task<bool> DeleteFileAsync(string blobName)
    {
        if (blobName.Contains("..") || blobName.StartsWith("/"))
        {
            _logger.LogWarning("Invalid blob name for deletion: {BlobName}", blobName);
            return false;
        }

        var blobClient = _containerClient.GetBlobClient(blobName);
        var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

        if (response.Value)
            _logger.LogInformation("Deleted blob '{BlobName}'", blobName);
        else
            _logger.LogWarning("Blob '{BlobName}' was not found for deletion.", blobName);

        return response.Value;
    }

    public async Task<bool> FileExistsAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        var response = await blobClient.ExistsAsync();
        return response.Value;
    }
}
