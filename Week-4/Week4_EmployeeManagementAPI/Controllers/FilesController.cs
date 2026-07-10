using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week3_EmployeeManagementAPI.Interfaces;
using Week3_EmployeeManagementAPI.Responses;

namespace Week3_EmployeeManagementAPI.Controllers
{
    /// <summary>
    /// File Management API — v1.
    /// Provides file upload, download, listing, and deletion.
    /// Validates file size and extension before storing on disk.
    /// Sends email notification on successful upload.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FilesController> _logger;
        private readonly IWebHostEnvironment _environment;

        public FilesController(
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<FilesController> logger,
            IWebHostEnvironment environment)
        {
            _emailService  = emailService;
            _configuration = configuration;
            _logger        = logger;
            _environment   = environment;
        }

        // ─────────────────────────────────────────────────────────────
        // POST /api/v1/files/upload
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Upload a file to the server.
        /// </summary>
        /// <remarks>
        /// **Validation rules:**
        /// - Maximum file size: 5 MB (configurable in appsettings.json)
        /// - Allowed extensions: .pdf, .doc, .docx, .jpg, .jpeg, .png, .txt, .xlsx
        ///
        /// An email notification is sent on successful upload.
        /// </remarks>
        /// <param name="file">The file to upload.</param>
        /// <returns>Upload result with file metadata.</returns>
        /// <response code="200">File uploaded successfully.</response>
        /// <response code="400">Validation failed (no file, too large, bad extension).</response>
        [HttpPost("upload")]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [RequestSizeLimit(10_485_760)] // 10 MB hard limit at Kestrel level
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.BadRequestResponse("No file was uploaded or file is empty."));
            }

            // Read config
            var maxSize = _configuration.GetValue<long>("FileUpload:MaxFileSizeBytes", 5_242_880);
            var allowedExtensions = _configuration.GetValue<string>("FileUpload:AllowedExtensions", ".pdf,.doc,.docx,.jpg,.jpeg,.png,.txt,.xlsx")!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => e.ToLowerInvariant())
                .ToHashSet();
            var storagePath = _configuration.GetValue<string>("FileUpload:StoragePath", "Uploads")!;

            // Validate file size
            if (file.Length > maxSize)
            {
                return BadRequest(ApiResponse<object>.BadRequestResponse(
                    $"File size ({file.Length / 1024.0 / 1024.0:F2} MB) exceeds the maximum allowed size ({maxSize / 1024.0 / 1024.0:F2} MB)."));
            }

            // Validate extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<object>.BadRequestResponse(
                    $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", allowedExtensions)}"));
            }

            // Build save path: wwwroot/Uploads/{unique_name}
            var uploadsDir = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), storagePath);
            Directory.CreateDirectory(uploadsDir);

            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsDir, uniqueFileName);

            // Save file to disk
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File '{OriginalName}' uploaded as '{StoredName}' ({Size} bytes).",
                file.FileName, uniqueFileName, file.Length);

            // Send email notification (fire-and-forget style — errors are logged, not thrown)
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "admin@company.com";
            await _emailService.SendEmailAsync(
                userEmail,
                "File Upload Notification",
                $"<h3>File Uploaded Successfully</h3>" +
                $"<p><strong>Original Name:</strong> {file.FileName}</p>" +
                $"<p><strong>Size:</strong> {file.Length / 1024.0:F1} KB</p>" +
                $"<p><strong>Uploaded At:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");

            var result = new
            {
                OriginalFileName = file.FileName,
                StoredFileName   = uniqueFileName,
                SizeBytes        = file.Length,
                SizeMB           = $"{file.Length / 1024.0 / 1024.0:F2} MB",
                Extension        = extension,
                UploadedAt       = DateTime.UtcNow,
                DownloadUrl      = $"/api/v1/files/download/{uniqueFileName}"
            };

            return Ok(ApiResponse<object>.SuccessResponse(result, "File uploaded successfully."));
        }

        // ─────────────────────────────────────────────────────────────
        // GET /api/v1/files/download/{fileName}
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Download a previously uploaded file by its stored file name.
        /// </summary>
        /// <param name="fileName">The stored file name (GUID-based).</param>
        /// <returns>The file content.</returns>
        /// <response code="200">File returned successfully.</response>
        /// <response code="404">File not found.</response>
        [HttpGet("download/{fileName}")]
        [Authorize(Policy = "AllRoles")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult Download(string fileName)
        {
            // Sanitize filename — prevent path traversal
            fileName = Path.GetFileName(fileName);

            var storagePath = _configuration.GetValue<string>("FileUpload:StoragePath", "Uploads")!;
            var uploadsDir  = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), storagePath);
            var filePath    = Path.Combine(uploadsDir, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(ApiResponse<object>.NotFoundResponse($"File '{fileName}' was not found."));
            }

            var contentType = GetContentType(fileName);
            var fileBytes   = System.IO.File.ReadAllBytes(filePath);

            _logger.LogInformation("File '{FileName}' downloaded.", fileName);
            return File(fileBytes, contentType, fileName);
        }

        // ─────────────────────────────────────────────────────────────
        // GET /api/v1/files
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// List all uploaded files with metadata.
        /// </summary>
        /// <returns>List of uploaded file metadata.</returns>
        /// <response code="200">File list returned.</response>
        [HttpGet]
        [Authorize(Policy = "AdminOrManager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult ListFiles()
        {
            var storagePath = _configuration.GetValue<string>("FileUpload:StoragePath", "Uploads")!;
            var uploadsDir  = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), storagePath);

            if (!Directory.Exists(uploadsDir))
            {
                return Ok(ApiResponse<object>.SuccessResponse(Array.Empty<object>(), "No files have been uploaded yet."));
            }

            var files = new DirectoryInfo(uploadsDir)
                .GetFiles()
                .Select(f => new
                {
                    FileName    = f.Name,
                    SizeBytes   = f.Length,
                    SizeMB      = $"{f.Length / 1024.0 / 1024.0:F2} MB",
                    Extension   = f.Extension,
                    UploadedAt  = f.CreationTimeUtc,
                    DownloadUrl = $"/api/v1/files/download/{f.Name}"
                })
                .OrderByDescending(f => f.UploadedAt)
                .ToList();

            return Ok(ApiResponse<object>.SuccessResponse(files, $"{files.Count} file(s) found."));
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE /api/v1/files/{fileName}
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Delete an uploaded file by its stored file name. Admin only.
        /// </summary>
        /// <param name="fileName">The stored file name to delete.</param>
        /// <returns>Deletion confirmation.</returns>
        /// <response code="200">File deleted.</response>
        /// <response code="404">File not found.</response>
        [HttpDelete("{fileName}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult Delete(string fileName)
        {
            fileName = Path.GetFileName(fileName);

            var storagePath = _configuration.GetValue<string>("FileUpload:StoragePath", "Uploads")!;
            var uploadsDir  = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), storagePath);
            var filePath    = Path.Combine(uploadsDir, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(ApiResponse<object>.NotFoundResponse($"File '{fileName}' was not found."));
            }

            System.IO.File.Delete(filePath);
            _logger.LogInformation("File '{FileName}' deleted by user '{User}'.", fileName, User.Identity?.Name);

            return Ok(ApiResponse.NoContentResponse($"File '{fileName}' deleted successfully."));
        }

        // ─────────────────────────────────────────────────────────────
        // Helper: map extensions → MIME types
        // ─────────────────────────────────────────────────────────────
        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf"  => "application/pdf",
                ".doc"  => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".txt"  => "text/plain",
                _       => "application/octet-stream"
            };
        }
    }
}
