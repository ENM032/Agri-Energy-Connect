using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication2.Areas.Identity.Data;

namespace WebApplication2.Controllers
{
    public class SetupController : Controller
    {
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<SetupController> _logger;

        public SetupController(
            UserManager<WebApplication2User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<SetupController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            try
            {
                // Check if admin user already exists
                var existingAdmin = await _userManager.FindByEmailAsync("admin@aec.com");
                if (existingAdmin != null)
                {
                    return Json(new { success = false, message = "Admin user already exists" });
                }

                // Create admin user
                var adminUser = new WebApplication2User
                {
                    UserName = "admin@aec.com",
                    Email = "admin@aec.com",
                    EmailConfirmed = true,
                    Displayname = "Administrator"
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin123!");
                
                if (result.Succeeded)
                {
                    // Assign Admin role
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    
                    _logger.LogInformation("Admin user created successfully");
                    return Json(new { success = true, message = "Admin user created successfully. Email: admin@aec.com, Password: Admin123!" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Failed to create admin user: {errors}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin user");
                return Json(new { success = false, message = "An error occurred while creating admin user" });
            }
        }
    }
}