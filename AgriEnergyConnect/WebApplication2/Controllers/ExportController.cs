using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Models.DTOs;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class ExportController : Controller
    {
        private readonly IDataExportService _dataExportService;
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly ILogger<ExportController> _logger;

        public ExportController(
            IDataExportService dataExportService,
            WebApplication2Context context,
            UserManager<WebApplication2User> userManager,
            ILogger<ExportController> logger)
        {
            _dataExportService = dataExportService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Export products data
        /// </summary>
        /// <param name="format">Export format (csv or excel)</param>
        /// <param name="category">Optional category filter</param>
        /// <param name="userId">Optional user filter (for farmers to export their own products)</param>
        /// <returns>File download</returns>
        [HttpGet]
        public async Task<IActionResult> Products(string format = "csv", string? category = null, string? userId = null)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                var isSupportEmployee = await _userManager.IsInRoleAsync(currentUser, "Support Employee");

                // Build query
                var query = _context.Products.AsQueryable();

                // Apply filters based on user role
                if (!isAdmin && !isSupportEmployee)
                {
                    // Farmers can only export their own products
                    query = query.Where(p => p.UserId == currentUser.Id);
                }
                else if (!string.IsNullOrEmpty(userId))
                {
                    // Admin/Support can filter by specific user
                    query = query.Where(p => p.UserId == userId);
                }

                // Apply category filter if specified
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                var products = await query.OrderBy(p => p.Name).ToListAsync();

                byte[] fileContent;
                string fileName;

                if (format.ToLower() == "excel")
                {
                    fileContent = await _dataExportService.ExportProductsToExcelAsync(products);
                    fileName = $"products_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                }
                else
                {
                    fileContent = await _dataExportService.ExportProductsToCsvAsync(products);
                    fileName = $"products_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                }

                _logger.LogInformation("User {UserId} exported {Count} products in {Format} format", 
                    currentUser.Id, products.Count, format);

                return File(fileContent, _dataExportService.GetMimeType(format), fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products data");
                return StatusCode(500, "An error occurred while exporting data");
            }
        }

        /// <summary>
        /// Export users data (Admin only)
        /// </summary>
        /// <param name="format">Export format (csv or excel)</param>
        /// <returns>File download</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users(string format = "csv")
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.UserName)
                    .ToListAsync();

                byte[] fileContent;
                string fileName;

                if (format.ToLower() == "excel")
                {
                    fileContent = await _dataExportService.ExportUsersToExcelAsync(users);
                    fileName = $"users_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                }
                else
                {
                    fileContent = await _dataExportService.ExportUsersToCsvAsync(users);
                    fileName = $"users_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                }

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("Admin {UserId} exported {Count} users in {Format} format", 
                    currentUser?.Id, users.Count, format);

                return File(fileContent, _dataExportService.GetMimeType(format), fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users data");
                return StatusCode(500, "An error occurred while exporting data");
            }
        }

        /// <summary>
        /// Export analytics data
        /// </summary>
        /// <param name="format">Export format (csv or excel)</param>
        /// <returns>File download</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Support Employee")]
        public async Task<IActionResult> Analytics(string format = "csv")
        {
            try
            {
                // Generate analytics data
                var totalUsers = await _context.Users.CountAsync();
                var totalProducts = await _context.Products.CountAsync();
                var activeUsers = await _context.Users
                    .Where(u => u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.Now)
                    .CountAsync();
                var productsThisMonth = await _context.Products
                    .Where(p => p.ProductDate.Month == DateTime.Now.Month && 
                               p.ProductDate.Year == DateTime.Now.Year)
                    .CountAsync();

                // Generate monthly data for the last 12 months
                var monthlyData = new List<MonthlyDataDto>();
                for (int i = 11; i >= 0; i--)
                {
                    var date = DateTime.Now.AddMonths(-i);
                    var monthUsers = await _context.Users
                        .Where(u => u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.Now)
                        .CountAsync();
                    var monthProducts = await _context.Products
                        .Where(p => p.ProductDate.Month == date.Month && 
                                   p.ProductDate.Year == date.Year)
                        .CountAsync();
                    
                    monthlyData.Add(new MonthlyDataDto
                    {
                        Month = date.ToString("yyyy-MM"),
                        Users = monthUsers,
                        Products = monthProducts
                    });
                }

                var analyticsData = new AnalyticsDto
                {
                    TotalUsers = totalUsers,
                    TotalProducts = totalProducts,
                    ActiveUsers = activeUsers,
                    ProductsThisMonth = productsThisMonth,
                    MonthlyData = monthlyData
                };

                byte[] fileContent;
                string fileName;

                if (format.ToLower() == "excel")
                {
                    fileContent = await _dataExportService.ExportAnalyticsToExcelAsync(analyticsData);
                    fileName = $"analytics_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                }
                else
                {
                    fileContent = await _dataExportService.ExportAnalyticsToCsvAsync(analyticsData);
                    fileName = $"analytics_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                }

                var currentUser = await _userManager.GetUserAsync(User);
                _logger.LogInformation("User {UserId} exported analytics data in {Format} format", 
                    currentUser?.Id, format);

                return File(fileContent, _dataExportService.GetMimeType(format), fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics data");
                return StatusCode(500, "An error occurred while exporting data");
            }
        }

        /// <summary>
        /// Get available export formats
        /// </summary>
        /// <returns>JSON with available formats</returns>
        [HttpGet]
        public IActionResult GetFormats()
        {
            var formats = new[]
            {
                new { value = "csv", text = "CSV" },
                new { value = "excel", text = "Excel" }
            };

            return Json(formats);
        }

        /// <summary>
        /// Get available product categories for filtering
        /// </summary>
        /// <returns>JSON with available categories</returns>
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.Category))
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return Json(new string[0]);
            }
        }
    }
}