using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebApplication2.Services
{
    /// <summary>
    /// Interface for email service functionality
    /// </summary>
    public interface IEmailService : IEmailSender
    {
        /// <summary>
        /// Send email with HTML content
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlMessage">HTML email content</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        
        /// <summary>
        /// Send notification email to user
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="message">Email message</param>
        /// <param name="isHtml">Whether the message is HTML formatted</param>
        /// <returns>Task representing the async operation</returns>
        Task SendNotificationAsync(string email, string subject, string message, bool isHtml = false);
        
        /// <summary>
        /// Send welcome email to new users
        /// </summary>
        /// <param name="email">User email address</param>
        /// <param name="userName">User name</param>
        /// <returns>Task representing the async operation</returns>
        Task SendWelcomeEmailAsync(string email, string userName);
    }
}