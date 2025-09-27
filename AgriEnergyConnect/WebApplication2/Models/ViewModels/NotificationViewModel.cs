using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// View model for displaying notifications
    /// </summary>
    public class NotificationViewModel
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        
        public string Message { get; set; } = string.Empty;
        
        public string Type { get; set; } = string.Empty; // Info, Warning, Success, Error
        
        public bool IsRead { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? ReadAt { get; set; }
        
        // Display properties
        public string TypeDisplayName => Type switch
        {
            "Info" => "Information",
            "Warning" => "Warning",
            "Success" => "Success",
            "Error" => "Error",
            _ => "Notification"
        };
        
        public string TypeIcon => Type switch
        {
            "Info" => "fas fa-info-circle",
            "Warning" => "fas fa-exclamation-triangle",
            "Success" => "fas fa-check-circle",
            "Error" => "fas fa-times-circle",
            _ => "fas fa-bell"
        };
        
        public string TypeClass => Type switch
        {
            "Info" => "alert-info",
            "Warning" => "alert-warning",
            "Success" => "alert-success",
            "Error" => "alert-danger",
            _ => "alert-secondary"
        };
        
        public string TypeBadgeClass => Type switch
        {
            "Info" => "badge-info",
            "Warning" => "badge-warning",
            "Success" => "badge-success",
            "Error" => "badge-danger",
            _ => "badge-secondary"
        };
        
        public string FormattedCreatedAt => CreatedAt.ToString("MMM dd, yyyy HH:mm");
        
        public string FormattedReadAt => ReadAt?.ToString("MMM dd, yyyy HH:mm") ?? "Not read";
        
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
                
                return FormattedCreatedAt;
            }
        }
        
        public string ReadStatusClass => IsRead ? "notification-read" : "notification-unread";
        
        public string TruncatedMessage => Message.Length > 100 ? Message.Substring(0, 100) + "..." : Message;
    }
    
    /// <summary>
    /// View model for notification listing with filtering
    /// </summary>
    public class NotificationIndexViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; } = new();
        
        public string? TypeFilter { get; set; }
        
        public string? StatusFilter { get; set; } // "Read", "Unread", "All"
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public int CurrentPage { get; set; } = 1;
        
        public int TotalPages { get; set; }
        
        public int TotalNotifications { get; set; }
        
        public int UnreadCount { get; set; }
        
        public bool UnreadOnly { get; set; }
        
        public int PageSize { get; set; } = 10;
        
        public List<SelectListItem> AvailableTypes { get; set; } = new();
        
        public List<SelectListItem> AvailableStatuses { get; set; } = new();
        
        public bool HasFilters => !string.IsNullOrEmpty(TypeFilter) || 
                                 !string.IsNullOrEmpty(StatusFilter) || 
                                 StartDate.HasValue || 
                                 EndDate.HasValue;
        
        public bool HasUnreadNotifications => UnreadCount > 0;
        
        public string UnreadCountDisplay => UnreadCount > 99 ? "99+" : UnreadCount.ToString();
    }
    
    /// <summary>
    /// View model for creating notifications (Admin)
    /// </summary>
    public class CreateNotificationViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Message is required")]
        [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
        public string Message { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; } = "Info";
        
        public string? TargetUserId { get; set; }
        
        public string? TargetRole { get; set; }
        
        public bool SendToAll { get; set; } = false;
        
        public bool SendEmail { get; set; } = false;
        
        public List<string> AvailableTypes { get; set; } = new List<string>
        {
            "Info",
            "Warning",
            "Success",
            "Error"
        };
        
        public List<string> AvailableRoles { get; set; } = new List<string>
        {
            "Admin",
            "SupportEmployee",
            "Farmer"
        };
        
        public List<UserDto> AvailableUsers { get; set; } = new List<UserDto>();
        
        public string TargetDescription
        {
            get
            {
                if (SendToAll) return "All users";
                if (!string.IsNullOrEmpty(TargetRole)) return $"All {TargetRole}s";
                if (!string.IsNullOrEmpty(TargetUserId)) return "Specific user";
                return "No target selected";
            }
        }
    }
    
    /// <summary>
    /// View model for notification statistics (Admin)
    /// </summary>
    public class NotificationStatsViewModel
    {
        public int TotalNotifications { get; set; }
        
        public int UnreadNotifications { get; set; }
        
        public int ReadNotifications { get; set; }
        
        public int InfoNotifications { get; set; }
        
        public int WarningNotifications { get; set; }
        
        public int SuccessNotifications { get; set; }
        
        public int ErrorNotifications { get; set; }
        
        public DateTime? LastNotificationDate { get; set; }
        
        public List<NotificationViewModel> RecentNotifications { get; set; } = new List<NotificationViewModel>();
        
        public double ReadPercentage => TotalNotifications > 0 ? (double)ReadNotifications / TotalNotifications * 100 : 0;
        
        public string FormattedLastNotificationDate => LastNotificationDate?.ToString("MMM dd, yyyy HH:mm") ?? "No notifications";
    }
}