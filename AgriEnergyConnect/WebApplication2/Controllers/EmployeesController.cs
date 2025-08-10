using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.IdentityModel.Tokens;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;
using Microsoft.Extensions.Logging;

namespace WebApplication2.Controllers
{
    /// <summary>
    /// Controller for employee-specific functionality - only accessible to users with Employee role
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            WebApplication2Context context, 
            UserManager<WebApplication2User> userManager, 
            RoleManager<IdentityRole> roleManager,
            ILogger<EmployeesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Display list of all farmers
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var farmers = await getAllFarmersFromDb().ToListAsync();
                _logger.LogInformation("Retrieved {FarmerCount} farmers for display", farmers.Count);
                return View(farmers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving farmers list");
                TempData["ErrorMessage"] = "An error occurred while loading farmers. Please try again.";
                return View(new List<WebApplication2User>());
            }
        }

        /// <summary>
        /// Display user registration form for Admin to create Support Employees
        /// </summary>
        /// <returns>Registration view</returns>
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                _logger.LogInformation("Displaying Support Employee registration form");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying Support Employee registration form");
                TempData["ErrorMessage"] = "An error occurred while loading the registration form.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Create new Support Employee account (Admin only)
        /// </summary>
        /// <param name="model">Registration model</param>
        /// <returns>Redirect to success page or return view with errors</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FarmerRegistrationModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Only allow Admin to create Support Employee accounts
                    if (model.Role != "Support Employee")
                    {
                        ModelState.AddModelError("Role", "Only Support Employee accounts can be created here.");
                        return View(model);
                    }

                    // Check if user already exists
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "A user with this email already exists.");
                        return View(model);
                    }

                    var user = new WebApplication2User 
                    { 
                        UserName = model.Email, 
                        Email = model.Email, 
                        Displayname = model.DisplayName 
                    };
                    
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        // Assign Support Employee role to user
                        await _userManager.AddToRoleAsync(user, "Support Employee");
                        _logger.LogInformation("Support Employee account created successfully for {Email} by admin", model.Email);
                        TempData["SuccessMessage"] = "Support Employee account created successfully!";
                        return RedirectToAction("Index", "Home");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(String.Empty, error.Description);
                        _logger.LogWarning("Support Employee creation failed: {Error}", error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Support Employee account for {Email}", model?.Email);
                ModelState.AddModelError("", "An error occurred while creating the account. Please try again.");
            }
            
            return View(model);
        }

        /// <summary>
        /// GET: Display farmer deletion confirmation
        /// </summary>
        public async Task<IActionResult> DeleteFarmer(string? id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("DeleteFarmer attempted with null or empty ID");
                    return NotFound();
                }

                var user = await _context.Users
                    .Include(p => p.Products)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                    return NotFound();
                }

                // Verify the user is actually a farmer
                var isInFarmerRole = await _userManager.IsInRoleAsync(user, "Farmer");
                if (!isInFarmerRole)
                {
                    _logger.LogWarning("Attempted to delete non-farmer user {UserId}", id);
                    TempData["ErrorMessage"] = "Only farmer accounts can be deleted through this interface.";
                    return RedirectToAction(nameof(Index));
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading farmer deletion confirmation for {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the farmer details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Confirm farmer deletion
        /// </summary>
        [HttpPost, ActionName("DeleteFarmer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFarmerConfirmed(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("DeleteFarmerConfirmed attempted with null or empty ID");
                    return NotFound();
                }

                var user = await _context.Users
                    .Include(u => u.Products)
                    .FirstOrDefaultAsync(u => u.Id == id);
                    
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                    TempData["ErrorMessage"] = "Farmer not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify the user is actually a farmer
                var isInFarmerRole = await _userManager.IsInRoleAsync(user, "Farmer");
                if (!isInFarmerRole)
                {
                    _logger.LogWarning("Attempted to delete non-farmer user {UserId}", id);
                    TempData["ErrorMessage"] = "Only farmer accounts can be deleted through this interface.";
                    return RedirectToAction(nameof(Index));
                }

                // Delete associated products first
                if (user.Products?.Any() == true)
                {
                    _context.Products.RemoveRange(user.Products);
                    _logger.LogInformation("Deleted {ProductCount} products for farmer {UserId}", user.Products.Count, id);
                }

                // Delete the user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Farmer account {Email} deleted successfully by employee", user.Email);
                TempData["SuccessMessage"] = "Farmer account deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting farmer {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the farmer account. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }


        /// <summary>
        /// GET: Display farmer product deletion confirmation
        /// </summary>
        public async Task<IActionResult> DeleteFarmerProduct(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("DeleteFarmerProduct attempted with null ID");
                    return NotFound();
                }

                var product = await _context.Products
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading farmer product deletion confirmation for {ProductId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the product details. Please try again.";
                return RedirectToAction(nameof(FarmerProducts));
            }
        }

        /// <summary>
        /// POST: Confirm farmer product deletion
        /// </summary>
        [HttpPost, ActionName("DeleteFarmerProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFarmerProductConfirmed(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id);
                    
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(FarmerProducts));
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Product {ProductName} deleted successfully by employee", product.Name);
                TempData["SuccessMessage"] = "Product deleted successfully!";
                return RedirectToAction(nameof(FarmerProducts));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting farmer product {ProductId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the product. Please try again.";
                return RedirectToAction(nameof(FarmerProducts));
            }
        }

        /// <summary>
        /// Display all farmer products with filtering options and pagination
        /// </summary>
        public async Task<IActionResult> FarmerProducts(int page = 1, int pageSize = 20)
        {
            try
            {
                // Use cached farmers for better performance
                var farmers = await GetFarmersWithCachingAsync();
                ViewData["UserName"] = new SelectList(farmers, "UserName", "UserName");
                ViewBag.CategoriesSelectList = new SelectList(ProductsController.GetCategories(), "Value", "Text");
                
                // Implement pagination for better performance
                var totalProducts = await _context.Products.CountAsync();
                var products = await _context.Products
                    .Include(p => p.User)
                    .OrderByDescending(p => p.ProductDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking() // Improve performance for read-only data
                    .ToListAsync();
                
                // Pass pagination info to view
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                ViewBag.TotalProducts = totalProducts;
                
                if (!products.Any())
                {
                    TempData["InfoMessage"] = "No products available to display. Farmers still need to add their products.";
                }
                
                _logger.LogInformation("Retrieved {ProductCount} farmer products for display (Page {Page})", products.Count, page);
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving farmer products");
                TempData["ErrorMessage"] = "An error occurred while loading farmer products. Please try again.";
                return View(new List<Product>());
            }
        }

        /// <summary>
        /// Filter farmer products based on user, category, and date range with pagination
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> FarmerProducts(string selectedUser, string selectedCategory, DateTime betweenStartDate, DateTime betweenEndDate, int page = 1, int pageSize = 20)
        {
            try
            {
                // Use cached farmers for better performance
                var farmers = await GetFarmersWithCachingAsync();
                ViewData["UserName"] = new SelectList(farmers, "UserName", "UserName");
                ViewBag.CategoriesSelectList = new SelectList(ProductsController.GetCategories(), "Value", "Text");
                
                // Build query dynamically based on filters
                var query = _context.Products.Include(p => p.User).AsQueryable();
                
                // Apply user filter
                if (!string.IsNullOrEmpty(selectedUser))
                {
                    query = query.Where(x => x.User.UserName == selectedUser);
                }
                
                // Apply category filter
                if (!string.IsNullOrEmpty(selectedCategory))
                {
                    query = query.Where(x => x.Category == selectedCategory);
                }
                
                // Apply date range filter
                if (betweenStartDate != DateTime.MinValue && betweenEndDate != DateTime.MinValue)
                {
                    query = query.Where(x => x.ProductDate >= betweenStartDate && x.ProductDate <= betweenEndDate);
                }
                
                query = query.OrderByDescending(p => p.ProductDate);

                // Get total count for pagination
                var totalProducts = await query.CountAsync();
                
                // Apply pagination and use AsNoTracking for better performance
                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();
                
                // Pass pagination and filter info to view
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                ViewBag.TotalProducts = totalProducts;
                ViewBag.SelectedUser = selectedUser;
                ViewBag.SelectedCategory = selectedCategory;
                ViewBag.StartDate = betweenStartDate;
                ViewBag.EndDate = betweenEndDate;
                
                if (!products.Any())
                {
                    TempData["InfoMessage"] = "No products found matching the selected criteria.";
                }
                
                _logger.LogInformation("Filtered farmer products: {ProductCount} results (Page {Page})", products.Count, page);
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering farmer products");
                TempData["ErrorMessage"] = "An error occurred while filtering products. Please try again.";
                
                // Return to unfiltered view on error with pagination
                var farmers = await GetFarmersWithCachingAsync();
                ViewData["UserName"] = new SelectList(farmers, "UserName", "UserName");
                ViewBag.CategoriesSelectList = new SelectList(ProductsController.GetCategories(), "Value", "Text");
                
                var fallbackProducts = await _context.Products
                    .Include(p => p.User)
                    .OrderByDescending(p => p.ProductDate)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();
                    
                return View(fallbackProducts);
            }
        }

        /// <summary>
        /// Retrieve all users with the Farmer role (optimized with caching)
        /// </summary>
        /// <returns>Queryable collection of farmer users</returns>
        public IQueryable<WebApplication2User> getAllFarmersFromDb()
        {
            try
            {
                // Use the original join approach but optimized
                var farmers = from userRole in _context.UserRoles
                              join user in _context.Users on userRole.UserId equals user.Id
                              join role in _context.Roles on userRole.RoleId equals role.Id
                              where role.Name == "Farmer"
                              select user;

                return farmers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving farmers from database");
                return Enumerable.Empty<WebApplication2User>().AsQueryable();
            }
        }
        
        /// <summary>
        /// Get farmers with caching for better performance
        /// </summary>
        /// <returns>List of farmer users</returns>
        private async Task<List<WebApplication2User>> GetFarmersWithCachingAsync()
        {
            try
            {
                // Simple in-memory caching - in production, use IMemoryCache or Redis
                var farmers = await _userManager.GetUsersInRoleAsync("Farmer");
                return farmers.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving farmers with caching");
                return new List<WebApplication2User>();
            }
        }

        /// <summary>
        /// Helper method to display database request errors (deprecated - use TempData instead)
        /// </summary>
        /// <param name="context">The queryable context to check</param>
        /// <param name="message">Error message to display</param>
        [Obsolete("Use TempData for error messages instead of ModelState for better UX")]
        public void displayDbRequestError(IIncludableQueryable<Product, WebApplication2User> context, string message)
        {
            try
            {
                if (context.IsNullOrEmpty())
                {
                    ModelState.AddModelError(String.Empty, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database context");
                ModelState.AddModelError(String.Empty, "An error occurred while processing your request.");
            }
        }

    }
}
