using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            WebApplication2Context context,
            UserManager<WebApplication2User> userManager,
            ILogger<AnalyticsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var analytics = await GetAnalyticsData();
                return View(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics");
                return View(new AnalyticsViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRegistrationData()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userRoles = new Dictionary<string, string>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userRoles[user.Id] = roles.FirstOrDefault() ?? "Unknown";
                }

                // Since CreatedAt is not available, we'll use current month data
                var currentDate = DateTime.Now;
                var registrationData = new List<object>
                {
                    new
                    {
                        Month = $"{currentDate.Year}-{currentDate.Month:D2}",
                        Count = users.Count,
                        Farmers = userRoles.Values.Count(r => r == "Farmer"),
                        SupportEmployees = userRoles.Values.Count(r => r == "Support Employee"),
                        Admins = userRoles.Values.Count(r => r == "Admin")
                    }
                };

                return Json(registrationData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user registration data");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductData()
        {
            try
            {
                var productData = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        RecentProducts = g.Count(p => p.ProductDate >= DateTime.Now.AddDays(-30))
                    })
                    .ToListAsync();

                return Json(productData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product data");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductTrendData()
        {
            try
            {
                var trendData = await _context.Products
                    .GroupBy(p => new { p.ProductDate.Year, p.ProductDate.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync();

                return Json(trendData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product trend data");
                return Json(new List<object>());
            }
        }

        private async Task<AnalyticsViewModel> GetAnalyticsData()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "Unknown";
            }

            var farmerCount = userRoles.Values.Count(r => r == "Farmer");
            var supportEmployeeCount = userRoles.Values.Count(r => r == "Support Employee");
            var adminCount = userRoles.Values.Count(r => r == "Admin");

            var recentProducts = await _context.Products
                .Where(p => p.ProductDate >= DateTime.Now.AddDays(-30))
                .CountAsync();

            var topCategories = await _context.Products
                .GroupBy(p => p.Category)
                .Select(g => new CategoryStats
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Count)
                .Take(5)
                .ToListAsync();

            // Since CreatedAt is not available, we'll show total new users as 0
            var recentUsers = 0;

            return new AnalyticsViewModel
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                FarmerCount = farmerCount,
                SupportEmployeeCount = supportEmployeeCount,
                AdminCount = adminCount,
                RecentProducts = recentProducts,
                RecentUsers = recentUsers,
                TopCategories = topCategories
            };
        }
    }

    public class AnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int FarmerCount { get; set; }
        public int SupportEmployeeCount { get; set; }
        public int AdminCount { get; set; }
        public int RecentProducts { get; set; }
        public int RecentUsers { get; set; }
        public List<CategoryStats> TopCategories { get; set; } = new List<CategoryStats>();
    }

    public class CategoryStats
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}