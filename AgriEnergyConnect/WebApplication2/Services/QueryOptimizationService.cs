using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Models.ViewModels;
using WebApplication2.Models.DTOs;
using WebApplication2.Areas.Identity.Data;

namespace WebApplication2.Services
{
    /// <summary>
    /// Service for optimized database queries with proper Entity Framework patterns
    /// </summary>
    public class QueryOptimizationService : IQueryOptimizationService
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly ILogger<QueryOptimizationService> _logger;

        public QueryOptimizationService(
            WebApplication2Context context,
            UserManager<WebApplication2User> userManager,
            ILogger<QueryOptimizationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get products with optimized query patterns
        /// </summary>
        public async Task<List<Product>> GetProductsOptimizedAsync(string? userId = null, bool includeUser = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Products.AsNoTracking();

            if (includeUser)
            {
                query = query.Include(p => p.User);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(p => p.UserId == userId);
            }

            return await query
                .OrderByDescending(p => p.ProductDate)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get filtered products with pagination and optimized query patterns
        /// </summary>
        public async Task<PagedResult<Product>> GetFilteredProductsOptimizedAsync(
            string? userId,
            string? searchName,
            string? categoryFilter,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page = 1,
            int pageSize = 10,
            bool includeUser = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Products.AsNoTracking();

            if (includeUser)
            {
                query = query.Include(p => p.User);
            }

            // Apply filters in order of selectivity (most selective first)
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(p => p.UserId == userId);
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                query = query.Where(p => p.Category == categoryFilter);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(p => p.ProductDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(p => p.ProductDate <= dateTo.Value);
            }

            if (!string.IsNullOrEmpty(searchName))
            {
                // Use EF.Functions.Like for better performance with indexes
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{searchName}%"));
            }

            query = query.OrderByDescending(p => p.ProductDate);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        /// <summary>
        /// Get products with pagination and optimized query patterns
        /// </summary>
        public async Task<PagedResult<Product>> GetProductsOptimizedAsync(string? userId = null, int page = 1, int pageSize = 10, bool includeUser = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Products.AsNoTracking();

            if (includeUser)
            {
                query = query.Include(p => p.User);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(p => p.UserId == userId);
            }

            query = query.OrderByDescending(p => p.ProductDate);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PagedResult<Product>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        /// <summary>
        /// Get filtered products with optimized query patterns
        /// </summary>
        public async Task<List<Product>> GetFilteredProductsOptimizedAsync(
            string? userId,
            string? searchName,
            string? categoryFilter,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool includeUser = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Products.AsNoTracking();

            if (includeUser)
            {
                query = query.Include(p => p.User);
            }

            // Apply filters in order of selectivity (most selective first)
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(p => p.UserId == userId);
            }

            if (!string.IsNullOrEmpty(categoryFilter))
            {
                query = query.Where(p => p.Category == categoryFilter);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(p => p.ProductDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(p => p.ProductDate <= dateTo.Value);
            }

            if (!string.IsNullOrEmpty(searchName))
            {
                // Use EF.Functions.Like for better performance with indexes
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{searchName}%"));
            }

            return await query
                .OrderByDescending(p => p.ProductDate)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get product analytics data with optimized aggregation
        /// </summary>
        public async Task<List<ChartDataPoint>> GetProductAnalyticsOptimizedAsync(int days, CancellationToken cancellationToken = default)
        {
            var startDate = DateTime.Today.AddDays(-days);
            var endDate = DateTime.Today.AddDays(1);

            // Use a single optimized query with proper indexing
            var productData = await _context.Products
                .AsNoTracking()
                .Where(p => p.ProductDate >= startDate && p.ProductDate < endDate)
                .GroupBy(p => p.ProductDate.Date)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key.ToString("MMM dd"),
                    Value = g.Count(),
                    Date = g.Key
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            // Fill in missing dates efficiently
            var allDates = Enumerable.Range(0, days)
                .Select(i => startDate.AddDays(i).Date)
                .ToHashSet();

            var existingDates = productData.Select(p => p.Date.Date).ToHashSet();
            var missingDates = allDates.Except(existingDates);

            var missingData = missingDates.Select(date => new ChartDataPoint
            {
                Label = date.ToString("MMM dd"),
                Value = 0,
                Date = date
            });

            return productData.Concat(missingData)
                .OrderBy(x => x.Date)
                .ToList();
        }

        /// <summary>
        /// Get category distribution with optimized grouping
        /// </summary>
        public async Task<List<CategoryDataPoint>> GetCategoryDistributionOptimizedAsync(CancellationToken cancellationToken = default)
        {
            // Single query with aggregation pushed to database
            var categoryData = await _context.Products
                .AsNoTracking()
                .GroupBy(p => p.Category)
                .Select(g => new CategoryDataPoint
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = 0 // Will be calculated in memory
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            // Calculate percentages efficiently
            var totalProducts = categoryData.Sum(c => c.Count);
            if (totalProducts > 0)
            {
                foreach (var item in categoryData)
                {
                    item.Percentage = Math.Round((double)item.Count / totalProducts * 100, 1);
                }
            }

            return categoryData;
        }

        /// <summary>
        /// Get user statistics with optimized role queries
        /// </summary>
        public async Task<List<RoleStatistic>> GetUserRoleStatisticsOptimizedAsync(CancellationToken cancellationToken = default)
        {
            // Use a single optimized query with joins
            var roleStats = await _context.UserRoles
                .AsNoTracking()
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .GroupBy(x => x.Name)
                .Select(g => new RoleStatistic
                {
                    RoleName = g.Key,
                    UserCount = g.Count()
                })
                .OrderByDescending(x => x.UserCount)
                .ToListAsync(cancellationToken);

            return roleStats;
        }

        /// <summary>
        /// Get dashboard analytics with single optimized query
        /// </summary>
        public async Task<DashboardAnalyticsViewModel> GetDashboardAnalyticsOptimizedAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var thirtyDaysAgo = today.AddDays(-30);

            // Execute multiple queries in parallel for better performance
            var totalUsersTask = _context.Users.AsNoTracking().CountAsync(cancellationToken);
            var totalProductsTask = _context.Products.AsNoTracking().CountAsync(cancellationToken);
            var recentProductsTask = _context.Products.AsNoTracking()
                .Where(p => p.ProductDate >= thirtyDaysAgo)
                .CountAsync(cancellationToken);
            var categoriesTask = _context.Products.AsNoTracking()
                .Select(p => p.Category)
                .Distinct()
                .CountAsync(cancellationToken);

            await Task.WhenAll(totalUsersTask, totalProductsTask, recentProductsTask, categoriesTask);

            return new DashboardAnalyticsViewModel
            {
                TotalUsers = await totalUsersTask,
                TotalProducts = await totalProductsTask,
                RecentProductsCount = await recentProductsTask,
                LastUpdated = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Get recent activity with optimized joins
        /// </summary>
        public async Task<List<RecentActivityItem>> GetRecentActivityOptimizedAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            // Optimized query with proper joins and projections
            var recentProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.User)
                .OrderByDescending(p => p.ProductDate)
                .Take(count)
                .Select(p => new RecentActivityItem
                {
                    Type = "Product Created",
                    Description = $"Product '{p.Name}' was created by {(p.User != null ? p.User.UserName : "Unknown User")}",
                    Timestamp = p.ProductDate,
                    UserId = p.UserId
                })
                .ToListAsync(cancellationToken);

            return recentProducts;
        }

        /// <summary>
        /// Get user registration trends with optimized date grouping
        /// </summary>
        public async Task<List<ChartDataPoint>> GetUserRegistrationTrendsOptimizedAsync(int days, CancellationToken cancellationToken = default)
        {
            var startDate = DateTime.Today.AddDays(-days);
            var endDate = DateTime.Today.AddDays(1);

            // Optimized query using RegistrationDate if available
            var registrationData = await _context.Users
                .AsNoTracking()
                .Where(u => u.RegistrationDate >= startDate && u.RegistrationDate < endDate)
                .GroupBy(u => u.RegistrationDate.Date)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key.ToString("MMM dd"),
                    Value = g.Count(),
                    Date = g.Key
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            // Fill in missing dates
            var allDates = Enumerable.Range(0, days)
                .Select(i => startDate.AddDays(i).Date)
                .ToHashSet();

            var existingDates = registrationData.Select(r => r.Date.Date).ToHashSet();
            var missingDates = allDates.Except(existingDates);

            var missingData = missingDates.Select(date => new ChartDataPoint
            {
                Label = date.ToString("MMM dd"),
                Value = 0,
                Date = date
            });

            return registrationData.Concat(missingData)
                .OrderBy(x => x.Date)
                .ToList();
        }
    }
}