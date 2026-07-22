using EmployeeService.Application.DTOs;
using EmployeeService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace EmployeeService.Controllers;

[ApiController]
[Route("api/employees/{employeeId:int}/documents")]
[Authorize]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentAppService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    // Max upload size: 10 MB
    private const long MaxFileSize = 10 * 1024 * 1024;

    public DocumentsController(IDocumentAppService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>Get all documents for an employee</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DocumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(int employeeId)
    {
        var documents = await _documentService.GetDocumentsByEmployeeAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<DocumentDto>>.SuccessResponse(documents));
    }

    /// <summary>Upload a file for an employee to Azure Blob Storage</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin,Manager")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    [ProducesResponseType(typeof(ApiResponse<DocumentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(int employeeId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.FailureResponse("No file was provided."));

        if (file.Length > MaxFileSize)
            return BadRequest(ApiResponse<object>.FailureResponse($"File size exceeds the {MaxFileSize / 1024 / 1024} MB limit."));

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _documentService.UploadDocumentAsync(
                employeeId,
                stream,
                file.FileName,
                file.ContentType);

            return CreatedAtAction(nameof(GetDocuments), new { employeeId },
                ApiResponse<DocumentDto>.SuccessResponse(result, "File uploaded successfully to Azure Blob Storage."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
        }
    }

    /// <summary>Download a document from Azure Blob Storage</summary>
    [HttpGet("{documentId:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int employeeId, int documentId)
    {
        var result = await _documentService.DownloadDocumentAsync(documentId);
        if (result == null)
            return NotFound(ApiResponse<object>.FailureResponse($"Document with ID {documentId} not found."));

        var (content, contentType, fileName) = result.Value;
        return File(content, contentType, fileName);
    }

    /// <summary>Delete a document from Azure Blob Storage and the database</summary>
    [HttpDelete("{documentId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int employeeId, int documentId)
    {
        var deleted = await _documentService.DeleteDocumentAsync(documentId);
        if (!deleted)
            return NotFound(ApiResponse<object>.FailureResponse($"Document with ID {documentId} not found."));

        return Ok(ApiResponse<object>.SuccessResponse(new { documentId }, "Document deleted successfully."));
    }
}
