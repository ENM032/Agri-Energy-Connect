using WebApplication2.Models.DTOs;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// ViewModel for role statistics used in query optimization
    /// </summary>
    public class RoleStatistic
    {
        public string RoleName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public double Percentage { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// ViewModel for dashboard analytics used in query optimization
    /// </summary>
    public class DashboardAnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalFarmers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalSupportEmployees { get; set; }
        public int RecentProductsCount { get; set; }
        public List<CategoryDataPoint> TopCategories { get; set; } = new List<CategoryDataPoint>();
        public List<RoleStatistic> RoleDistribution { get; set; } = new List<RoleStatistic>();
        public List<ChartDataPoint> ProductGrowthData { get; set; } = new List<ChartDataPoint>();
        public DateTime LastUpdated { get; set; }
        public double AverageResponseTime { get; set; }
        public int ErrorCount { get; set; }
    }

    /// <summary>
    /// ViewModel for user registration trends
    /// </summary>
    public class UserRegistrationTrend
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public string Period { get; set; } = string.Empty; // "Daily", "Weekly", "Monthly"
    }

    /// <summary>
    /// ViewModel for optimized product analytics
    /// </summary>
    public class ProductAnalyticsViewModel
    {
        public int TotalProducts { get; set; }
        public int ProductsThisMonth { get; set; }
        public int ProductsThisWeek { get; set; }
        public List<CategoryDataPoint> CategoryDistribution { get; set; } = new List<CategoryDataPoint>();
        public List<ChartDataPoint> MonthlyTrends { get; set; } = new List<ChartDataPoint>();
        public DateTime LastUpdated { get; set; }
    }
}