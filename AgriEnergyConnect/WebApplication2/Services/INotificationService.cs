using WebApplication2.Models;

namespace WebApplication2.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(string userId, string title, string message, NotificationType type, bool sendEmail = false);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int take = 50);
        Task<Notification?> GetNotificationByIdAsync(int id);
        Task<bool> MarkAsReadAsync(int notificationId, string userId);
        Task<bool> MarkAllAsReadAsync(string userId);
        Task<bool> DeleteNotificationAsync(int notificationId, string userId);
        Task<bool> DeleteAllNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
    }
}