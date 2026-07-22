using EmployeeService.Application.DTOs;
using EmployeeService.Domain.Entities;
using EmployeeService.Infrastructure.Azure;
using EmployeeService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeService.Application.Services;

public interface IDocumentAppService
{
    Task<DocumentDto> UploadDocumentAsync(int employeeId, Stream fileStream, string fileName, string contentType);
    Task<(Stream Content, string ContentType, string FileName)?> DownloadDocumentAsync(int documentId);
    Task<bool> DeleteDocumentAsync(int documentId);
    Task<IEnumerable<DocumentDto>> GetDocumentsByEmployeeAsync(int employeeId);
}

public class DocumentAppService : IDocumentAppService
{
    private readonly EmployeeDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<DocumentAppService> _logger;

    public DocumentAppService(
        EmployeeDbContext context,
        IBlobStorageService blobStorage,
        ILogger<DocumentAppService> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<DocumentDto> UploadDocumentAsync(
        int employeeId, Stream fileStream, string fileName, string contentType)
    {
        // Validate employee exists
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || !employee.IsActive)
            throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        // Upload to Azure Blob Storage
        var uploadResult = await _blobStorage.UploadFileAsync(fileStream, fileName, contentType, employeeId);

        // Save metadata to database
        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            FileName = uploadResult.BlobName,
            OriginalFileName = fileName,
            ContentType = contentType,
            FileSize = uploadResult.FileSize,
            BlobUrl = uploadResult.BlobUrl,
            BlobName = uploadResult.BlobName,
            UploadedAt = DateTime.UtcNow
        };

        _context.EmployeeDocuments.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document '{FileName}' uploaded for employee {EmployeeId}, DocumentId={DocumentId}",
            fileName, employeeId, document.Id);

        return MapToDto(document);
    }

    public async Task<(Stream Content, string ContentType, string FileName)?> DownloadDocumentAsync(int documentId)
    {
        var document = await _context.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found.", documentId);
            return null;
        }

        var result = await _blobStorage.DownloadFileAsync(document.BlobName);
        if (result == null)
        {
            _logger.LogWarning("Blob '{BlobName}' not found in storage for document {DocumentId}.",
                document.BlobName, documentId);
            return null;
        }

        return (result.Content, result.ContentType, document.OriginalFileName);
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        var document = await _context.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

        if (document == null) return false;

        // Delete from Azure Blob Storage
        await _blobStorage.DeleteFileAsync(document.BlobName);

        // Soft delete the database record
        document.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} deleted (blob: '{BlobName}').", documentId, document.BlobName);
        return true;
    }

    public async Task<IEnumerable<DocumentDto>> GetDocumentsByEmployeeAsync(int employeeId)
    {
        return await _context.EmployeeDocuments
            .Where(d => d.EmployeeId == employeeId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                EmployeeId = d.EmployeeId,
                FileName = d.FileName,
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                BlobUrl = d.BlobUrl,
                UploadedAt = d.UploadedAt
            })
            .ToListAsync();
    }

    private static DocumentDto MapToDto(EmployeeDocument d) => new()
    {
        Id = d.Id,
        EmployeeId = d.EmployeeId,
        FileName = d.FileName,
        OriginalFileName = d.OriginalFileName,
        ContentType = d.ContentType,
        FileSize = d.FileSize,
        BlobUrl = d.BlobUrl,
        UploadedAt = d.UploadedAt
    };
}
