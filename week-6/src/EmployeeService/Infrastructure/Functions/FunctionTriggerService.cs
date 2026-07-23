using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmployeeService.Infrastructure.Functions;

/// <summary>
/// Called by the Employee API after create/update/delete operations.
/// Sends a fire-and-forget HTTP request to the Azure Functions notification endpoint.
///
/// In production:  Points to https://<function-app>.azurewebsites.net/api/employee-notification
/// In development: Points to http://localhost:7071/api/employee-notification (Functions local runtime)
///
/// Design: fire-and-forget with try/catch so a Function failure never breaks the main API.
/// </summary>
public interface IFunctionTriggerService
{
    Task TriggerEmployeeNotificationAsync(
        int employeeId, string firstName, string lastName,
        string email, string position, string departmentName,
        string eventType, string? additionalMessage = null);
}

public class FunctionTriggerService : IFunctionTriggerService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionTriggerService> _logger;

    public FunctionTriggerService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FunctionTriggerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task TriggerEmployeeNotificationAsync(
        int employeeId, string firstName, string lastName,
        string email, string position, string departmentName,
        string eventType, string? additionalMessage = null)
    {
        var functionUrl = _configuration["AzureFunctions:NotificationUrl"];
        var functionKey = _configuration["AzureFunctions:FunctionKey"];

        if (string.IsNullOrEmpty(functionUrl))
        {
            _logger.LogDebug(
                "AzureFunctions:NotificationUrl not configured — skipping notification trigger for Employee {EmployeeId}",
                employeeId);
            return;
        }

        var payload = new
        {
            employeeId,
            firstName,
            lastName,
            email,
            position,
            departmentName,
            eventType,
            additionalMessage,
            occurredAt = DateTime.UtcNow
        };

        try
        {
            var client = _httpClientFactory.CreateClient("AzureFunctions");

            var url = functionUrl;
            if (!string.IsNullOrEmpty(functionKey))
                url += $"?code={functionKey}";

            var json = JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully triggered notification function for Employee {EmployeeId} — Event: {EventType}",
                    employeeId, eventType);
            }
            else
            {
                _logger.LogWarning(
                    "Notification function returned {StatusCode} for Employee {EmployeeId}",
                    response.StatusCode, employeeId);
            }
        }
        catch (Exception ex)
        {
            // Fire-and-forget: log the error but don't propagate it to the API caller
            _logger.LogWarning(ex,
                "Failed to trigger notification function for Employee {EmployeeId}. " +
                "The employee operation completed successfully — only the notification failed.",
                employeeId);
        }
    }
}
