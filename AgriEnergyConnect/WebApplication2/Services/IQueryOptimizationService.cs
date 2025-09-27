using WebApplication2.Models;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Models.ViewModels;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Services
{
    /// <summary>
    /// Service for optimized database queries with proper Entity Framework patterns
    /// </summary>
    public interface IQueryOptimizationService
    {
        /// <summary>
        /// Get products with optimized query patterns
        /// </summary>
        Task<List<Product>> GetProductsOptimizedAsync(string? userId = null, bool includeUser = false, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get products with pagination and optimized query patterns
        /// </summary>
        Task<PagedResult<Product>> GetProductsOptimizedAsync(string? userId = null, int page = 1, int pageSize = 10, bool includeUser = false, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get filtered products with optimized query patterns
        /// </summary>
        Task<List<Product>> GetFilteredProductsOptimizedAsync(
            string? userId,
            string? searchName,
            string? categoryFilter,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool includeUser = false,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Get filtered products with pagination and optimized query patterns
        /// </summary>
        Task<PagedResult<Product>> GetFilteredProductsOptimizedAsync(
            string? userId = null,
            string? searchName = null,
            string? categoryFilter = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int page = 1,
            int pageSize = 10,
            bool includeUser = false,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get product analytics data with optimized aggregation
        /// </summary>
        Task<List<ChartDataPoint>> GetProductAnalyticsOptimizedAsync(int days, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get category distribution with optimized grouping
        /// </summary>
        Task<List<CategoryDataPoint>> GetCategoryDistributionOptimizedAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get user statistics with optimized role queries
        /// </summary>
        Task<List<RoleStatistic>> GetUserRoleStatisticsOptimizedAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get dashboard analytics with single optimized query
        /// </summary>
        Task<DashboardAnalyticsViewModel> GetDashboardAnalyticsOptimizedAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get recent activity with optimized joins
        /// </summary>
        Task<List<RecentActivityItem>> GetRecentActivityOptimizedAsync(int count = 10, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get user registration trends with optimized date grouping
        /// </summary>
        Task<List<ChartDataPoint>> GetUserRegistrationTrendsOptimizedAsync(int days, CancellationToken cancellationToken = default);
    }
}