using System.Net;
using System.Text.Json;
using EmployeeManagement.AzureFunctions.Models;
using EmployeeManagement.AzureFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.AzureFunctions.Functions;

/// <summary>
/// HTTP TRIGGER FUNCTION
/// ─────────────────────────────────────────────────────────────────────────────
/// What is an HTTP Trigger?
///   An HTTP Trigger starts a Function when it receives an HTTP request —
///   just like an ASP.NET Core controller action. The difference is:
///   - No always-running server. Azure spins up compute only when a request arrives.
///   - Scales to zero automatically — costs nothing when idle.
///   - Can scale to thousands of instances per second automatically.
///
/// Route:  POST /api/employee-notification
/// Auth:   Function key required in header: x-functions-key or in query ?code=
///
/// Called by the Employee API after:
///   - Creating a new employee
///   - Updating an employee
///   - Uploading a document
///
/// Flow:
///   Employee API → POST /api/employee-notification → Azure Function
///                   ↓
///            Validates the request
///                   ↓
///            Sends email via SendGrid (or logs in dev)
///                   ↓
///            Returns 200 OK with processing result
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class EmployeeNotificationFunction
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<EmployeeNotificationFunction> _logger;

    public EmployeeNotificationFunction(
        INotificationService notificationService,
        ILogger<EmployeeNotificationFunction> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [Function("EmployeeNotification")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "employee-notification")]
        HttpRequestData req)
    {
        _logger.LogInformation("EmployeeNotification HTTP trigger fired at {Time}", DateTime.UtcNow);

        // ── 1. Read & parse request body ──────────────────────────────────
        EmployeeNotificationRequest? notification;
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return await BadRequestAsync(req, "Request body is empty.");

            notification = JsonSerializer.Deserialize<EmployeeNotificationRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (notification == null)
                return await BadRequestAsync(req, "Could not deserialize request body.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in request body");
            return await BadRequestAsync(req, $"Invalid JSON: {ex.Message}");
        }

        // ── 2. Validate required fields ───────────────────────────────────
        var validationErrors = ValidateRequest(notification);
        if (validationErrors.Any())
            return await BadRequestAsync(req, "Validation failed.", validationErrors);

        // ── 3. Process the notification ───────────────────────────────────
        try
        {
            _logger.LogInformation(
                "Processing {EventType} notification for Employee {EmployeeId} ({Email})",
                notification.EventType, notification.EmployeeId, notification.Email);

            await _notificationService.SendEmployeeNotificationAsync(notification);

            _logger.LogInformation(
                "Notification processed successfully for Employee {EmployeeId}", notification.EmployeeId);

            // ── 4. Return 200 OK ──────────────────────────────────────────
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var result = FunctionResponse.Ok(
                $"Notification sent for {notification.EventType} event — Employee {notification.EmployeeId}",
                new
                {
                    employeeId   = notification.EmployeeId,
                    eventType    = notification.EventType.ToString(),
                    email        = notification.Email,
                    processedAt  = DateTime.UtcNow
                },
                "EmployeeNotification");

            await response.WriteStringAsync(JsonSerializer.Serialize(result,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification for Employee {EmployeeId}",
                notification.EmployeeId);
            return await InternalErrorAsync(req, "Notification processing failed. See Application Insights for details.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HTTP GET: health check for this specific function
    // Route:  GET /api/employee-notification/health
    // ─────────────────────────────────────────────────────────────────────────
    [Function("EmployeeNotificationHealth")]
    public async Task<HttpResponseData> Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "employee-notification/health")]
        HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new
        {
            status      = "Healthy",
            function    = "EmployeeNotification",
            timestamp   = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Unknown"
        }));
        return response;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<string> ValidateRequest(EmployeeNotificationRequest req)
    {
        var errors = new List<string>();
        if (req.EmployeeId <= 0) errors.Add("EmployeeId must be a positive integer.");
        if (string.IsNullOrWhiteSpace(req.FirstName)) errors.Add("FirstName is required.");
        if (string.IsNullOrWhiteSpace(req.LastName)) errors.Add("LastName is required.");
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            errors.Add("A valid Email is required.");
        return errors;
    }

    private static async Task<HttpResponseData> BadRequestAsync(
        HttpRequestData req, string message, List<string>? errors = null)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(
            FunctionResponse.Fail(message + (errors?.Any() == true ? " " + string.Join("; ", errors) : ""),
                "EmployeeNotification"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return response;
    }

    private static async Task<HttpResponseData> InternalErrorAsync(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(
            FunctionResponse.Fail(message, "EmployeeNotification"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return response;
    }
}
