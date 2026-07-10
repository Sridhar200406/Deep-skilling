using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Week3_EmployeeManagementAPI.Interfaces;

namespace Week3_EmployeeManagementAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("EmailService: Preparing to send email to '{To}' with subject '{Subject}'.", toEmail, subject);

            var smtpSection = _configuration.GetSection("SmtpSettings");
            var server = smtpSection["Server"] ?? "localhost";
            var port = int.Parse(smtpSection["Port"] ?? "25");
            var senderEmail = smtpSection["SenderEmail"] ?? "no-reply@company.com";
            var senderName = smtpSection["SenderName"] ?? "Employee Management System";
            var username = smtpSection["Username"] ?? "";
            var password = smtpSection["Password"] ?? "";
            var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "false");

            try
            {
                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(senderEmail, senderName);
                mailMessage.To.Add(new MailAddress(toEmail));
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;

                using var smtpClient = new SmtpClient(server, port);
                smtpClient.EnableSsl = enableSsl;
                
                if (!string.IsNullOrWhiteSpace(username))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(username, password);
                }
                else
                {
                    smtpClient.UseDefaultCredentials = true;
                }

                // Attempt to send
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("EmailService: Email successfully sent to '{To}'.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailService: Failed to send email to '{To}' due to: {Error}. Subject: {Subject}", toEmail, ex.Message, subject);
                // Do not rethrow in order to prevent SMTP network issues from blocking core business workflows during local runs.
            }
        }
    }
}
