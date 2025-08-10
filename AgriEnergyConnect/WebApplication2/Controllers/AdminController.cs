using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly WebApplication2Context _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<WebApplication2User> userManager,
            RoleManager<IdentityRole> roleManager,
            WebApplication2Context context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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

        /// <summary>
        /// Display form to create a new admin user
        /// </summary>
        /// <returns>Create admin view</returns>
        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View();
        }

        /// <summary>
        /// Create a new admin user
        /// </summary>
        /// <param name="model">Admin creation model</param>
        /// <returns>Redirect to manage users or view with errors</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View(model);
                }

                // Create new admin user
                var adminUser = new WebApplication2User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    Displayname = model.DisplayName
                };

                var result = await _userManager.CreateAsync(adminUser, model.Password);
                
                if (result.Succeeded)
                {
                    // Assign Admin role
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    
                    _logger.LogInformation($"New admin user created: {model.Email}");
                    TempData["SuccessMessage"] = $"Admin user '{model.DisplayName}' created successfully.";
                    return RedirectToAction(nameof(ManageUsers));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin user");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the admin user.");
                return View(model);
            }
        }

        /// <summary>
        /// Display user deletion confirmation
        /// </summary>
        /// <param name="id">User ID to delete</param>
        /// <returns>Delete confirmation view</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("DeleteUser attempted with null or empty ID");
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                    return NotFound();
                }

                // Get user roles for display
                var roles = await _userManager.GetRolesAsync(user);
                ViewBag.UserRoles = roles;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user deletion confirmation for {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading user details.";
                return RedirectToAction(nameof(ManageUsers));
            }
        }

        /// <summary>
        /// Confirm user deletion
        /// </summary>
        /// <param name="id">User ID to delete</param>
        /// <returns>Redirect to manage users</returns>
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("DeleteUserConfirmed attempted with null or empty ID");
                return RedirectToAction(nameof(ManageUsers));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(ManageUsers));
                }

                // Prevent admin from deleting themselves
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == user.Id)
                {
                    _logger.LogWarning("Admin attempted to delete their own account {UserId}", id);
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(ManageUsers));
                }

                var userEmail = user.Email;
                var userRoles = await _userManager.GetRolesAsync(user);
                var userDisplayName = user.Displayname;

                // Delete associated products if user is a farmer
                if (userRoles.Contains("Farmer"))
                {
                    var userProducts = await _context.Products.Where(p => p.UserId == user.Id).ToListAsync();
                    if (userProducts.Any())
                    {
                        _context.Products.RemoveRange(userProducts);
                        _logger.LogInformation("Deleted {ProductCount} products for user {UserId}", userProducts.Count, id);
                    }
                }

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User account {Email} ({Roles}) deleted successfully by admin", userEmail, string.Join(", ", userRoles));
                    TempData["SuccessMessage"] = $"User '{userDisplayName}' ({string.Join(", ", userRoles)}) deleted successfully.";
                }
                else
                {
                    _logger.LogError("Failed to delete user {UserId}: {Errors}", id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }

                return RedirectToAction(nameof(ManageUsers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the user account. Please try again.";
                return RedirectToAction(nameof(ManageUsers));
            }
        }
    }

    /// <summary>
    /// View model for creating admin users
    /// </summary>
    public class CreateAdminViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}