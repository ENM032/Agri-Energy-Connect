using System.ComponentModel.DataAnnotations;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// View model for admin dashboard with comprehensive statistics
    /// </summary>
    public class DashboardViewModel
    {
        // User Statistics
        public int TotalUsers { get; set; }
        public int TotalFarmers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalSupportEmployees { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsersToday { get; set; }
        
        // Product Statistics
        public int TotalProducts { get; set; }
        public int NewProductsThisMonth { get; set; }
        public int ProductsToday { get; set; }
        public string MostPopularCategory { get; set; } = string.Empty;
        
        // Notification Statistics
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int NotificationsToday { get; set; }
        
        // System Health
        public string SystemStatus { get; set; } = "Healthy";
        public string SystemHealth { get; set; } = "Good";
        public double SystemUptime { get; set; }
        public DateTime LastSystemCheck { get; set; }
        
        // Recent Activity
        public List<RecentActivityItem> RecentActivities { get; set; } = new List<RecentActivityItem>();
        public List<ProductViewModel> RecentProducts { get; set; } = new List<ProductViewModel>();
        public List<UserProfileViewModel> RecentUsers { get; set; } = new List<UserProfileViewModel>();
        public List<NotificationViewModel> RecentNotifications { get; set; } = new List<NotificationViewModel>();
        
        // Chart Data
        public List<ChartDataPoint> UserGrowthData { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> ProductGrowthData { get; set; } = new List<ChartDataPoint>();
        public List<CategoryDataPoint> CategoryDistribution { get; set; } = new List<CategoryDataPoint>();
        public List<RoleStatisticsDto> RoleDistribution { get; set; } = new List<RoleStatisticsDto>();
        
        // Performance Metrics
        public double AverageResponseTime { get; set; }
        public int TotalRequests { get; set; }
        public int ErrorCount { get; set; }
        public double ErrorRate { get; set; }
        
        // Display Properties
        public string SystemStatusClass => SystemStatus switch
        {
            "Healthy" => "text-success",
            "Warning" => "text-warning",
            "Critical" => "text-danger",
            _ => "text-muted"
        };
        
        public string SystemStatusIcon => SystemStatus switch
        {
            "Healthy" => "fas fa-check-circle",
            "Warning" => "fas fa-exclamation-triangle",
            "Critical" => "fas fa-times-circle",
            _ => "fas fa-question-circle"
        };
        
        public string FormattedUptime
        {
            get
            {
                var uptime = TimeSpan.FromHours(SystemUptime);
                if (uptime.TotalDays >= 1)
                    return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
                if (uptime.TotalHours >= 1)
                    return $"{uptime.Hours}h {uptime.Minutes}m";
                return $"{uptime.Minutes}m";
            }
        }
        
        public string FormattedLastCheck => LastSystemCheck.ToString("MMM dd, yyyy HH:mm");
        
        public double UserGrowthPercentage
        {
            get
            {
                if (TotalUsers == 0) return 0;
                var previousMonth = TotalUsers - NewUsersThisMonth;
                return previousMonth > 0 ? (double)NewUsersThisMonth / previousMonth * 100 : 100;
            }
        }
        
        public double ProductGrowthPercentage
        {
            get
            {
                if (TotalProducts == 0) return 0;
                var previousMonth = TotalProducts - NewProductsThisMonth;
                return previousMonth > 0 ? (double)NewProductsThisMonth / previousMonth * 100 : 100;
            }
        }
        
        public bool HasRecentActivity => RecentActivities.Any();
        public bool HasRecentProducts => RecentProducts.Any();
        public bool HasRecentUsers => RecentUsers.Any();
        public bool HasRecentNotifications => RecentNotifications.Any();
        
        public string ErrorRateClass => ErrorRate switch
        {
            <= 1.0 => "text-success",
            <= 5.0 => "text-warning",
            _ => "text-danger"
        };
    }
    
    /// <summary>
    /// View model for recent activity items
    /// </summary>
    public class RecentActivityItem
    {
        public string Type { get; set; } = string.Empty; // "User", "Product", "Notification", "System"
        public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted", "Login"
        public string Description { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Icon { get; set; }
        public string? Url { get; set; }
        
        public string TypeIcon => Type switch
        {
            "User" => "fas fa-user",
            "Product" => "fas fa-box",
            "Notification" => "fas fa-bell",
            "System" => "fas fa-cog",
            _ => "fas fa-info-circle"
        };
        
        public string TypeClass => Type switch
        {
            "User" => "text-primary",
            "Product" => "text-success",
            "Notification" => "text-warning",
            "System" => "text-info",
            _ => "text-muted"
        };
        
        public string FormattedTimestamp => Timestamp.ToString("MMM dd, HH:mm");
        
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Timestamp;
                
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                
                return FormattedTimestamp;
            }
        }
    }
    
    /// <summary>
    /// View model for analytics dashboard
    /// </summary>
    public class AnalyticsDashboardViewModel
    {
        public DashboardAnalyticsDto Analytics { get; set; } = new DashboardAnalyticsDto();
        
        public List<ChartDataPoint> MonthlyUserData { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> MonthlyProductData { get; set; } = new List<ChartDataPoint>();
        public List<CategoryDataPoint> CategoryData { get; set; } = new List<CategoryDataPoint>();
        public List<RoleStatisticsDto> RoleData { get; set; } = new List<RoleStatisticsDto>();
        
        public PerformanceMetricsDto PerformanceMetrics { get; set; } = new PerformanceMetricsDto();
        public SystemHealthDto SystemHealth { get; set; } = new SystemHealthDto();
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CategoryFilter { get; set; }
        public string? RoleFilter { get; set; }
        
        public List<string> AvailableCategories { get; set; } = new List<string>();
        public List<string> AvailableRoles { get; set; } = new List<string>();
        
        public bool HasFilters => StartDate.HasValue || EndDate.HasValue || 
                                 !string.IsNullOrEmpty(CategoryFilter) || 
                                 !string.IsNullOrEmpty(RoleFilter);
        
        public string DateRangeDisplay
        {
            get
            {
                if (StartDate.HasValue && EndDate.HasValue)
                    return $"{StartDate.Value:MMM dd} - {EndDate.Value:MMM dd, yyyy}";
                if (StartDate.HasValue)
                    return $"From {StartDate.Value:MMM dd, yyyy}";
                if (EndDate.HasValue)
                    return $"Until {EndDate.Value:MMM dd, yyyy}";
                return "All time";
            }
        }
    }
    
    /// <summary>
    /// View model for export functionality
    /// </summary>
    public class ExportDashboardViewModel
    {
        public List<string> AvailableFormats { get; set; } = new List<string> { "CSV", "Excel" };
        public List<string> AvailableDataTypes { get; set; } = new List<string> { "Products", "Users", "Analytics", "Notifications" };
        
        [Required(ErrorMessage = "Format is required")]
        public string Format { get; set; } = "CSV";
        
        [Required(ErrorMessage = "Data type is required")]
        public string DataType { get; set; } = "Products";
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CategoryFilter { get; set; }
        public string? RoleFilter { get; set; }
        
        public List<string> SelectedColumns { get; set; } = new List<string>();
        public List<string> AvailableColumns { get; set; } = new List<string>();
        
        public bool IncludeHeaders { get; set; } = true;
        public bool CompressFile { get; set; } = false;
        
        public string EstimatedFileSize { get; set; } = "Unknown";
        public int EstimatedRecordCount { get; set; }
    }
}