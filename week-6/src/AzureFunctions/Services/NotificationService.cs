using EmployeeManagement.AzureFunctions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmployeeManagement.AzureFunctions.Services;

public interface INotificationService
{
    Task SendEmployeeNotificationAsync(EmployeeNotificationRequest request);
    Task SendAdminReportAsync(string subject, string htmlContent, string recipientEmail);
}

/// <summary>
/// Sends email notifications via SendGrid.
/// If SendGrid API key is not configured, logs the notification instead (dev mode).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IConfiguration config, ILogger<NotificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmployeeNotificationAsync(EmployeeNotificationRequest request)
    {
        var subject = request.EventType switch
        {
            NotificationEventType.EmployeeCreated    => $"Welcome to {request.DepartmentName}, {request.FirstName}!",
            NotificationEventType.EmployeeUpdated    => $"Your profile has been updated, {request.FirstName}",
            NotificationEventType.EmployeeDeactivated => $"Account Notice — {request.FirstName} {request.LastName}",
            NotificationEventType.DocumentUploaded    => $"Document upload confirmed — {request.FirstName}",
            NotificationEventType.PasswordReset       => $"Password reset request — {request.FirstName}",
            _ => $"Employee Management Notification — {request.FirstName}"
        };

        var htmlContent = BuildEmployeeEmailHtml(request);

        await SendEmailAsync(request.Email, $"{request.FirstName} {request.LastName}", subject, htmlContent);
    }

    public async Task SendAdminReportAsync(string subject, string htmlContent, string recipientEmail)
    {
        var adminName = _config["Report:AdminName"] ?? "Admin";
        await SendEmailAsync(recipientEmail, adminName, subject, htmlContent);
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        var apiKey = _config["Notification:SendGridApiKey"];
        var fromEmail = _config["Notification:FromEmail"] ?? "noreply@employeemanagement.com";
        var fromName = _config["Notification:FromName"] ?? "Employee Management System";

        if (string.IsNullOrEmpty(apiKey))
        {
            // Dev mode — log instead of sending
            _logger.LogInformation(
                "[NOTIFICATION - DEV MODE] To: {ToEmail} | Subject: {Subject} | Body: {Body}",
                toEmail, subject, htmlContent);
            return;
        }

        try
        {
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail, toName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                _logger.LogError("SendGrid returned {StatusCode} for email to {ToEmail}", response.StatusCode, toEmail);
            }
            else
            {
                _logger.LogInformation("Email sent to {ToEmail} — Subject: {Subject}", toEmail, subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }

    private static string BuildEmployeeEmailHtml(EmployeeNotificationRequest req)
    {
        var eventDescription = req.EventType switch
        {
            NotificationEventType.EmployeeCreated     => $"Your employee account has been created in the Employee Management System.",
            NotificationEventType.EmployeeUpdated     => $"Your employee profile has been updated.",
            NotificationEventType.EmployeeDeactivated => $"Your employee account has been deactivated.",
            NotificationEventType.DocumentUploaded    => $"A document has been uploaded to your profile.",
            NotificationEventType.PasswordReset       => $"A password reset has been requested for your account.",
            _ => "An update has been made to your employee record."
        };

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
              <div style="background:#0078d4; padding:20px; border-radius:8px 8px 0 0;">
                <h1 style="color:white; margin:0; font-size:24px;">Employee Management System</h1>
              </div>
              <div style="background:#f8f9fa; padding:30px; border:1px solid #dee2e6; border-top:none;">
                <h2 style="color:#212529;">Hello, {req.FirstName}!</h2>
                <p style="color:#495057; font-size:16px;">{eventDescription}</p>
                <table style="width:100%; border-collapse:collapse; margin:20px 0;">
                  <tr style="background:#e9ecef;">
                    <td style="padding:10px; font-weight:bold; width:40%;">Employee ID</td>
                    <td style="padding:10px;">{req.EmployeeId}</td>
                  </tr>
                  <tr>
                    <td style="padding:10px; font-weight:bold;">Full Name</td>
                    <td style="padding:10px;">{req.FirstName} {req.LastName}</td>
                  </tr>
                  <tr style="background:#e9ecef;">
                    <td style="padding:10px; font-weight:bold;">Email</td>
                    <td style="padding:10px;">{req.Email}</td>
                  </tr>
                  <tr>
                    <td style="padding:10px; font-weight:bold;">Position</td>
                    <td style="padding:10px;">{req.Position}</td>
                  </tr>
                  <tr style="background:#e9ecef;">
                    <td style="padding:10px; font-weight:bold;">Department</td>
                    <td style="padding:10px;">{req.DepartmentName}</td>
                  </tr>
                  <tr>
                    <td style="padding:10px; font-weight:bold;">Event</td>
                    <td style="padding:10px;">{req.EventType}</td>
                  </tr>
                  <tr style="background:#e9ecef;">
                    <td style="padding:10px; font-weight:bold;">Processed At</td>
                    <td style="padding:10px;">{req.OccurredAt:yyyy-MM-dd HH:mm:ss} UTC</td>
                  </tr>
                </table>
                {(string.IsNullOrEmpty(req.AdditionalMessage) ? "" : $"<p style='color:#495057;'><strong>Note:</strong> {req.AdditionalMessage}</p>")}
                <hr style="border:none; border-top:1px solid #dee2e6; margin:20px 0;" />
                <p style="color:#6c757d; font-size:12px;">
                  This is an automated message from the Employee Management System.<br/>
                  Please do not reply to this email.
                </p>
              </div>
            </body>
            </html>
            """;
    }
}
