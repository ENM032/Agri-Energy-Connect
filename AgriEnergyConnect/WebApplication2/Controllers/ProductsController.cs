using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    // Only shows you this action if youre a employee [Authorize(Roles = "Employee")]
    // Only shows you this page if youre a farmer [Authorize(Roles = "Farmer")]
    [Authorize(Roles = "Farmer,Admin,Support Employee")]
    public class ProductsController : Controller
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly ILogger<ProductsController> _logger;
        
        public ProductsController(WebApplication2Context context, UserManager<WebApplication2User> userManager, ILogger<ProductsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: Products - Display user's products with filtering
        /// </summary>
        public async Task<IActionResult> Index(string searchName, string categoryFilter, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found for authenticated user");
                    return RedirectToAction("Login", "Account");
                }

                // Check if user is Support Employee or Admin to show all products
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                bool canViewAllProducts = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");

                // Build query with filters
                var query = _context.Products
                    .Include(p => p.User)
                    .AsQueryable();

                // Filter by user if not Support Employee or Admin
                if (!canViewAllProducts)
                {
                    query = query.Where(x => x.UserId == userId);
                }

                // Apply name filter
                if (!string.IsNullOrEmpty(searchName))
                {
                    query = query.Where(p => p.Name.Contains(searchName));
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    query = query.Where(p => p.Category == categoryFilter);
                }

                // Apply date range filter
                if (dateFrom.HasValue)
                {
                    query = query.Where(p => p.ProductDate >= dateFrom.Value);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(p => p.ProductDate <= dateTo.Value);
                }

                var products = await query
                    .OrderByDescending(p => p.ProductDate)
                    .ToListAsync();

                // Pass filter values to view for maintaining state
                ViewBag.SearchName = searchName;
                ViewBag.CategoryFilter = categoryFilter;
                ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

                _logger.LogInformation("Retrieved {ProductCount} products for user {UserId} with filters", products.Count, userId);
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for user");
                TempData["ErrorMessage"] = "An error occurred while loading your products. Please try again.";
                return View(new List<Product>());
            }
        }

        /// <summary>
        /// GET: Products/Create - Display create product form
        /// </summary>
        public IActionResult Create()
        {
            try
            {
                ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Value", "Text");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create product form");
                TempData["ErrorMessage"] = "An error occurred while loading the form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Products/Create - Create new product
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Category,ProductDate,UserId")] Product product)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found during product creation");
                    return RedirectToAction("Login", "Account");
                }

                // Ensure the UserId is set to the current user
                product.UserId = userId;

                // Validate product date is not in the future
                if (product.ProductDate > DateTime.Now)
                {
                    ModelState.AddModelError("ProductDate", "Production date cannot be in the future.");
                }

                if (ModelState.IsValid)
                {
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Product {ProductName} created successfully by user {UserId}", product.Name, userId);
                    TempData["SuccessMessage"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product {ProductName}", product?.Name);
                ModelState.AddModelError("", "An error occurred while creating the product. Please try again.");
            }

            ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Value", "Text", product.Category);
            return View(product);
        }

        /// <summary>
        /// GET: Products/Edit/5 - Display edit product form
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    _logger.LogWarning("Edit attempted with null ID");
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found during product edit");
                    return RedirectToAction("Login", "Account");
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found", id);
                    return NotFound();
                }

                // Check if user can edit this product (own products, or Support Employee/Admin can edit any)
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                bool canEditAllProducts = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
                
                if (!canEditAllProducts && product.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to edit product {ProductId} belonging to another user", userId, id);
                    TempData["ErrorMessage"] = "You can only edit your own products.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Value", "Text", product.Category);
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for product {ProductId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the product. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Products/Edit/5 - Update product
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,ProductDate,UserId")] Product product)
        {
            try
            {
                if (id != product.Id)
                {
                    _logger.LogWarning("Product ID mismatch: URL ID {UrlId}, Product ID {ProductId}", id, product.Id);
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found during product update");
                    return RedirectToAction("Login", "Account");
                }

                // Check if user can edit this product (own products, or Support Employee/Admin can edit any)
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                bool canEditAllProducts = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
                
                if (!canEditAllProducts && product.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update product {ProductId} belonging to another user", userId, id);
                    TempData["ErrorMessage"] = "You can only edit your own products.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate product date is not in the future
                if (product.ProductDate > DateTime.Now)
                {
                    ModelState.AddModelError("ProductDate", "Production date cannot be in the future.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(product);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Product {ProductName} updated successfully by user {UserId}", product.Name, userId);
                        TempData["SuccessMessage"] = "Product updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!ProductExists(product.Id))
                        {
                            _logger.LogWarning("Product {ProductId} no longer exists during update", product.Id);
                            return NotFound();
                        }
                        else
                        {
                            _logger.LogError(ex, "Concurrency error updating product {ProductId}", product.Id);
                            ModelState.AddModelError("", "The product was modified by another user. Please reload and try again.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                ModelState.AddModelError("", "An error occurred while updating the product. Please try again.");
            }

            ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Value", "Text", product.Category);
            //ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", product.UserId);
            return View(product);
        }

        /// <summary>
        /// GET: Products/Delete/5 - Display delete confirmation
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null || _context.Products == null)
                {
                    _logger.LogWarning("Delete attempted with null ID or context");
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found during product delete");
                    return RedirectToAction("Login", "Account");
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                    return NotFound();
                }

                // Check if user can delete this product (own products, or Support Employee/Admin can delete any)
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                bool canDeleteAllProducts = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
                
                if (!canDeleteAllProducts && product.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete product {ProductId} belonging to another user", userId, id);
                    TempData["ErrorMessage"] = "You can only delete your own products.";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation for product {ProductId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the product. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Products/Delete/5 - Confirm product deletion
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (_context.Products == null)
                {
                    _logger.LogError("Product context is null during deletion");
                    return Problem("Entity set 'WebApplication2Context.Products' is null.");
                }

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found during product deletion");
                    return RedirectToAction("Login", "Account");
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check user permissions for deletion
                var currentUser = await _userManager.GetUserAsync(User);
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                
                // Allow deletion if user is Admin, Support Employee, or owns the product
                if (!userRoles.Contains("Admin") && !userRoles.Contains("Support Employee") && product.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to delete product {ProductId} without permission", userId, id);
                    TempData["ErrorMessage"] = "You don't have permission to delete this product.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Product {ProductName} deleted by user {UserId} with roles {Roles}", product.Name, userId, string.Join(", ", userRoles));
                TempData["SuccessMessage"] = "Product deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the product. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // Removed PopulateLocalUserIdVariable method - use GetCurrentUserId() instead

        /// <summary>
        /// Get the current user's ID with null checking
        /// </summary>
        /// <returns>Current user ID or null if not found</returns>
        private string GetCurrentUserId()
        {
            try
            {
                return _userManager.GetUserId(this.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user ID");
                return null;
            }
        }

        /// <summary>
        /// Get available product categories
        /// </summary>
        /// <returns>List of category options for dropdown</returns>
        public static List<SelectListItem> GetCategories()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Cereals", Text = "Cereals" },
                new SelectListItem { Value = "Seeds", Text = "Seeds" },
                new SelectListItem { Value = "Pulses", Text = "Pulses" },
                new SelectListItem { Value = "Fruits", Text = "Fruits" },
                new SelectListItem { Value = "Vegetables", Text = "Vegetables" },
                new SelectListItem { Value = "Herbs & Spices", Text = "Herbs & Spices" }
            };
        }
    }
}
