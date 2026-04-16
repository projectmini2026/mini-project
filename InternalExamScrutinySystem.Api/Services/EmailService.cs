using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace InternalExamScrutinySystem.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var server = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderName = _config["EmailSettings:SenderName"];
            var enableSsl = bool.Parse(_config["EmailSettings:EnableSsl"] ?? "true");

            if (string.IsNullOrEmpty(username) || username.Contains("your-email") || string.IsNullOrEmpty(password) || password == "your-app-password")
            {
                Console.WriteLine("********************************************************************************");
                Console.WriteLine("[EMAIL ACTION REQUIRED] SMTP credentials are not configured in appsettings.json.");
                Console.WriteLine($"[SKIPPING SEND] Recipient: {to}");
                Console.WriteLine("Please update 'Username' and 'Password' in appsettings.json to enable emails.");
                Console.WriteLine("********************************************************************************");
                return;
            }

            using var client = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                Timeout = 10000 // 10 second timeout
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail!, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mailMessage.To.Add(to);

            Console.WriteLine($"[EMAIL ATTEMPT] Sending to {to} via {server}:{port}...");
            await client.SendMailAsync(mailMessage);
            Console.WriteLine($"[EMAIL SUCCESS] Sent to {to}");
        }
        catch (SmtpException smtpEx)
        {
            Console.WriteLine($"[EMAIL SMTP ERROR] Code: {smtpEx.StatusCode} | Message: {smtpEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] Failed to send to {to}: {ex.GetType().Name} - {ex.Message}");
        }
    }
}
