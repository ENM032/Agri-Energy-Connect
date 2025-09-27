namespace WebApplication2.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for dashboard analytics overview
    /// </summary>
    public class DashboardAnalyticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalFarmers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalSupportEmployees { get; set; }
        public int RecentProductsCount { get; set; }
        public List<CategoryDataPoint> TopCategories { get; set; } = new List<CategoryDataPoint>();
        public DateTime LastUpdated { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for chart data points
    /// </summary>
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public DateTime Date { get; set; }
        public string? Color { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for category-based data points
    /// </summary>
    public class CategoryDataPoint
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string? Color { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for role statistics
    /// </summary>
    public class RoleStatisticsDto
    {
        public string Role { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string? Description { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for recent activity summary
    /// </summary>
    public class RecentActivityDto
    {
        public int NewProductsCount { get; set; }
        public int NewUsersCount { get; set; }
        public string Period { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ActivityItem> Activities { get; set; } = new List<ActivityItem>();
    }
    
    /// <summary>
    /// Data Transfer Object for individual activity items
    /// </summary>
    public class ActivityItem
    {
        public string Type { get; set; } = string.Empty; // "Product", "User", etc.
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Icon { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for performance metrics
    /// </summary>
    public class PerformanceMetricsDto
    {
        public double AverageResponseTime { get; set; }
        public int TotalRequests { get; set; }
        public int ErrorCount { get; set; }
        public double ErrorRate { get; set; }
        public DateTime MeasurementPeriodStart { get; set; }
        public DateTime MeasurementPeriodEnd { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for system health status
    /// </summary>
    public class SystemHealthDto
    {
        public string Status { get; set; } = "Healthy"; // "Healthy", "Warning", "Critical"
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public int ActiveConnections { get; set; }
        public DateTime LastChecked { get; set; }
        public List<HealthCheckItem> HealthChecks { get; set; } = new List<HealthCheckItem>();
    }
    
    /// <summary>
    /// Data Transfer Object for individual health check items
    /// </summary>
    public class HealthCheckItem
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public TimeSpan Duration { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for analytics filters
    /// </summary>
    public class AnalyticsFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Category { get; set; }
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
    
    /// <summary>
    /// Data Transfer Object for export requests
    /// </summary>
    public class ExportRequestDto
    {
        public string Format { get; set; } = "CSV"; // "CSV", "Excel", "PDF"
        public string DataType { get; set; } = string.Empty; // "Products", "Users", "Analytics"
        public AnalyticsFilterDto? Filters { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Data Transfer Object for analytics data export
    /// </summary>
    public class AnalyticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveUsers { get; set; }
        public int ProductsThisMonth { get; set; }
        public List<MonthlyDataDto> MonthlyData { get; set; } = new List<MonthlyDataDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Data Transfer Object for monthly analytics data
    /// </summary>
    public class MonthlyDataDto
    {
        public string Month { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Products { get; set; }
    }
}