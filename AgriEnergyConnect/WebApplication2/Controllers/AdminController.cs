using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    /// <summary>
    /// Controller for admin-specific functionality - only accessible to users with Admin role
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly WebApplication2Context _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<WebApplication2User> userManager,
            WebApplication2Context context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Admin dashboard with system overview
        /// </summary>
        /// <returns>Admin dashboard view</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                var totalUsers = await _userManager.Users.CountAsync();
                var totalProducts = await _context.Products.CountAsync();
                var farmers = await _userManager.GetUsersInRoleAsync("Farmer");
                var supportEmployees = await _userManager.GetUsersInRoleAsync("Support Employee");
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                var dashboardData = new
                {
                    TotalUsers = totalUsers,
                    TotalProducts = totalProducts,
                    FarmersCount = farmers.Count,
                    SupportEmployeesCount = supportEmployees.Count,
                    AdminsCount = admins.Count
                };

                ViewBag.DashboardData = dashboardData;
                _logger.LogInformation("Admin dashboard accessed");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Display all users with their roles
        /// </summary>
        /// <returns>Users management view</returns>
        public async Task<IActionResult> ManageUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userRoles = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userRoles.Add(new
                    {
                        User = user,
                        Roles = roles
                    });
                }

                ViewBag.UserRoles = userRoles;
                _logger.LogInformation("Admin accessed user management");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management");
                TempData["ErrorMessage"] = "An error occurred while loading user management.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Display all products with farmer information
        /// </summary>
        /// <returns>Products management view</returns>
        public async Task<IActionResult> ManageProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.User)
                    .OrderByDescending(p => p.ProductDate)
                    .ToListAsync();

                _logger.LogInformation("Admin accessed product management");
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product management");
                TempData["ErrorMessage"] = "An error occurred while loading product management.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}