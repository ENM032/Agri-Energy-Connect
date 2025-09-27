using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Services
{
    public class NotificationService : INotificationService
    {
        private readonly WebApplication2Context _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            WebApplication2Context context,
            IEmailService emailService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(string userId, string title, string message, NotificationType type, bool sendEmail = false)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);

                // Send email notification if requested
                if (sendEmail)
                {
                    await SendEmailNotificationAsync(notification);
                }

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int take = 50)
        {
            try
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Notification?> GetNotificationByIdAsync(int id)
        {
            try
            {
                return await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification {NotificationId}", id);
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return false;
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
                throw;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                if (!unreadNotifications.Any())
                {
                    return true;
                }

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return false;
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Notification {NotificationId} deleted for user {UserId}", notificationId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteAllNotificationsAsync(string userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();

                if (!notifications.Any())
                {
                    return true;
                }

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
                _logger.LogInformation("All notifications deleted for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all notifications for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                return await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                throw;
            }
        }

        private async Task SendEmailNotificationAsync(Notification notification)
        {
            try
            {
                var user = await _context.Users.FindAsync(notification.UserId);
                if (user?.Email != null)
                {
                    var subject = $"[AgriEnergyConnect] {notification.Title}";
                    var body = $"<h3>{notification.Title}</h3><p>{notification.Message}</p><p><small>Sent at {notification.CreatedAt:yyyy-MM-dd HH:mm} UTC</small></p>";
                    
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    _logger.LogInformation("Email notification sent to {Email} for notification {NotificationId}", user.Email, notification.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification for notification {NotificationId}", notification.Id);
                // Don't throw here as we don't want email failures to break notification creation
            }
        }
    }
}