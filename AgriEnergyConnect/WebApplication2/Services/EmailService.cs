using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace WebApplication2.Services
{
    /// <summary>
    /// Email service implementation using SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(_emailSettings.SmtpServer))
                {
                    _logger.LogWarning("Email service not configured. Email to {Email} with subject '{Subject}' was not sent.", email, subject);
                    return;
                }

                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    EnableSsl = _emailSettings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email} with subject '{Subject}'", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'", email, subject);
                throw;
            }
        }

        public async Task SendNotificationAsync(string email, string subject, string message, bool isHtml = false)
        {
            var emailContent = isHtml ? message : $"<p>{message}</p>";
            await SendEmailAsync(email, subject, emailContent);
        }

        public async Task SendWelcomeEmailAsync(string email, string userName)
        {
            var subject = "Welcome to Agri-Energy Connect!";
            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>Welcome to Agri-Energy Connect!</h2>
                    <p>Dear {userName},</p>
                    <p>Thank you for joining Agri-Energy Connect, the premier platform connecting farmers with green energy solutions.</p>
                    <p>You can now:</p>
                    <ul>
                        <li>List your agricultural products</li>
                        <li>Connect with energy providers</li>
                        <li>Access market analytics</li>
                        <li>Manage your profile and preferences</li>
                    </ul>
                    <p>Get started by logging into your account and exploring the platform.</p>
                    <p>Best regards,<br/>The Agri-Energy Connect Team</p>
                </div>";

            await SendEmailAsync(email, subject, htmlMessage);
        }
    }

    /// <summary>
    /// Email configuration settings
    /// </summary>
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string SmtpUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "Agri-Energy Connect";
    }
}