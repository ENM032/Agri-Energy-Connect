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
using WebApplication2.Models.ViewModels;
using WebApplication2.Models.DTOs;
using WebApplication2.Services;
using AutoMapper;

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
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly IPerformanceLoggingService _performanceLoggingService;
        private readonly ICachingService _cachingService;
        private readonly IQueryOptimizationService _queryOptimizationService;
        
        public ProductsController(WebApplication2Context context, UserManager<WebApplication2User> userManager, ILogger<ProductsController> logger, IFileUploadService fileUploadService, INotificationService notificationService, IMapper mapper, IPerformanceLoggingService performanceLoggingService, ICachingService cachingService, IQueryOptimizationService queryOptimizationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileUploadService = fileUploadService;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _performanceLoggingService = performanceLoggingService ?? throw new ArgumentNullException(nameof(performanceLoggingService));
            _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
            _queryOptimizationService = queryOptimizationService ?? throw new ArgumentNullException(nameof(queryOptimizationService));
        }

        /// <summary>
        /// GET: Products - Display user's products with filtering and pagination
        /// </summary>
        [ResponseCache(CacheProfileName = "ProductList")]
        public async Task<IActionResult> Index(string searchName, string categoryFilter, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 10)
        {
            using var tracker = _performanceLoggingService.StartTracking("ProductsController.Index", new { searchName, categoryFilter, dateFrom, dateTo, page, pageSize });
            
            try
            {
                // Ensure valid pagination parameters
                page = Math.Max(1, page);
                pageSize = Math.Max(1, Math.Min(50, pageSize)); // Limit max page size to 50
                
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

                // Create cache key based on user and filters
                var hasFilters = !string.IsNullOrEmpty(searchName) || !string.IsNullOrEmpty(categoryFilter) || dateFrom.HasValue || dateTo.HasValue;
                var cacheKey = hasFilters ? 
                    $"products_filtered_{userId}_{searchName}_{categoryFilter}_{dateFrom}_{dateTo}_{canViewAllProducts}_{page}_{pageSize}" :
                    $"{(canViewAllProducts ? CacheKeys.ProductsList : string.Format(CacheKeys.ProductsListByCategory, userId))}_{page}_{pageSize}";

                // Use optimized query service
                PagedResult<Product> pagedProducts;
                if (!hasFilters)
                {
                    // Use optimized service for basic product retrieval
                    pagedProducts = await _queryOptimizationService.GetProductsOptimizedAsync(canViewAllProducts ? null : userId, page, pageSize);
                }
                else
                {
                    // Use optimized service for filtered product retrieval
                    pagedProducts = await _queryOptimizationService.GetFilteredProductsOptimizedAsync(
                        canViewAllProducts ? null : userId,
                        searchName,
                        categoryFilter,
                        dateFrom,
                        dateTo,
                        page,
                        pageSize);
                }
                
                _performanceLoggingService.LogDatabaseQuery("ProductsQuery", tracker.ElapsedTime, pagedProducts.TotalCount);

                // Map to view model
                var productViewModels = _mapper.Map<List<ProductViewModel>>(pagedProducts.Items);
                
                var indexViewModel = new ProductIndexViewModel
                {
                    Products = productViewModels,
                    SearchName = searchName,
                    CategoryFilter = categoryFilter,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    Categories = GetCategories().Select(c => c.Text).ToList(),
                    CanViewAllProducts = canViewAllProducts,
                    CurrentPage = pagedProducts.Page,
                    TotalPages = pagedProducts.TotalPages,
                    PageSize = pagedProducts.PageSize,
                    TotalProducts = pagedProducts.TotalCount
                };

                _logger.LogInformation("Retrieved {ProductCount} products for user {UserId} with filters (page {Page} of {TotalPages})", pagedProducts.Items.Count, userId, page, pagedProducts.TotalPages);
                tracker.Complete();
                return View(indexViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for user");
                TempData["ErrorMessage"] = "An error occurred while loading your products. Please try again.";
                return View(new ProductIndexViewModel
                {
                    Products = new List<ProductViewModel>(),
                    Categories = GetCategories().Select(c => c.Text).ToList(),
                    CurrentPage = 1,
                    TotalPages = 1,
                    PageSize = pageSize,
                    TotalProducts = 0
                });
            }
        }

        /// <summary>
        /// GET: Products/Create - Display create product form
        /// </summary>
        public IActionResult Create()
        {
            try
            {
                var createViewModel = new ProductCreateViewModel
                {
                    Categories = GetCategories().Select(c => c.Text).ToList()
                };
                return View(createViewModel);
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
        public async Task<IActionResult> Create(ProductCreateViewModel model, IFormFile? productImage)
        {
            using var tracker = _performanceLoggingService.StartTracking("ProductsController.Create", new { productName = model?.Name, hasImage = productImage != null });
            
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found during product creation");
                    return RedirectToAction("Login", "Account");
                }

                // Map view model to entity
                var product = _mapper.Map<Product>(model);
                product.UserId = userId;

                // Validate product date is not in the future
                if (model.ProductDate > DateTime.Now)
                {
                    ModelState.AddModelError("ProductDate", "Production date cannot be in the future.");
                }

                // Handle file upload if provided
                if (productImage != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    var validationResult = _fileUploadService.ValidateFile(productImage, allowedExtensions, 5);
                    if (!validationResult.IsValid)
                    {
                        ModelState.AddModelError("productImage", validationResult.ErrorMessage);
                        model.Categories = GetCategories().Select(c => c.Text).ToList();
                        return View(model);
                    }

                    try
                    {
                        using var uploadTracker = _performanceLoggingService.StartTracking("ProductsController.Create.FileUpload", new { fileName = productImage.FileName, fileSize = productImage.Length });
                        var uploadResult = await _fileUploadService.UploadFileAsync(productImage, "products");
                        product.ImagePath = uploadResult;
                        product.ImageFileName = productImage.FileName;
                        uploadTracker.Complete();
                        _logger.LogInformation("Product image uploaded successfully: {ImagePath}", uploadResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading product image");
                        ModelState.AddModelError("productImage", "Failed to upload image. Please try again.");
                        model.Categories = GetCategories().Select(c => c.Text).ToList();
                        return View(model);
                    }
                }

                if (ModelState.IsValid)
                {
                    using var dbTracker = _performanceLoggingService.StartTracking("ProductsController.Create.DatabaseSave", new { productName = product.Name });
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    dbTracker.Complete();
                    
                    // Invalidate product cache
                    _cachingService.Remove(CacheKeys.ProductsList);
                    _cachingService.Remove(string.Format(CacheKeys.ProductsListByCategory, userId));
                    _cachingService.RemoveByPrefix("products_filtered_");
                    
                    // Create notification for successful product creation
                    await _notificationService.CreateNotificationAsync(
                        userId,
                        "Product Created",
                        $"Your product '{product.Name}' has been successfully created.",
                        NotificationType.Success
                    );
                    
                    _logger.LogInformation("Product {ProductName} created successfully by user {UserId}", product.Name, userId);
                    TempData["SuccessMessage"] = "Product created successfully!";
                    tracker.Complete();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product {ProductName}", model?.Name);
                ModelState.AddModelError("", "An error occurred while creating the product. Please try again.");
            }

            model.Categories = GetCategories().Select(c => c.Text).ToList();
            return View(model);
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
                if (currentUser == null)
                {
                    _logger.LogWarning("Current user not found during product edit");
                    return RedirectToAction("Login", "Account");
                }
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                bool canEditAllProducts = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
                
                if (!canEditAllProducts && product.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to edit product {ProductId} belonging to another user", userId, id);
                    TempData["ErrorMessage"] = "You can only edit your own products.";
                    return RedirectToAction(nameof(Index));
                }

                var editViewModel = _mapper.Map<ProductEditViewModel>(product);
                editViewModel.Categories = GetCategories().Select(c => c.Text).ToList();
                
                return View(editViewModel);
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
        public async Task<IActionResult> Edit(int id, ProductEditViewModel model, IFormFile? productImage)
        {
            try
            {
                if (id != model.Id)
                {
                    _logger.LogWarning("Product ID mismatch: URL ID {UrlId}, Product ID {ProductId}", id, model.Id);
                    return NotFound();
                }

                // Map view model to entity
                var product = _mapper.Map<Product>(model);

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found during product update");
                    return RedirectToAction("Login", "Account");
                }

                // Check if user can edit this product (own products, or Support Employee/Admin can edit any)
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Current user not found during product update");
                    return RedirectToAction("Login", "Account");
                }
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                bool canEditAllProducts = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
                
                if (!canEditAllProducts && model.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to update product {ProductId} belonging to another user", userId, id);
                    TempData["ErrorMessage"] = "You can only edit your own products.";
                    return RedirectToAction(nameof(Index));
                }

                // Get existing product to preserve current image if no new image is uploaded
                var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (existingProduct == null)
                {
                    _logger.LogWarning("Product {ProductId} not found during update", id);
                    return NotFound();
                }

                // Handle file upload if provided
                if (productImage != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                     var validationResult = _fileUploadService.ValidateFile(productImage, allowedExtensions, 5);
                    if (!validationResult.IsValid)
                    {
                        ModelState.AddModelError("productImage", validationResult.ErrorMessage);
                        model.Categories = GetCategories().Select(c => c.Text).ToList();
                        return View(model);
                    }

                    try
                    {
                        // Delete old image if it exists
                        if (!string.IsNullOrEmpty(existingProduct.ImagePath))
                        {
                            await _fileUploadService.DeleteFileAsync(existingProduct.ImagePath);
                            _logger.LogInformation("Old product image deleted: {ImagePath}", existingProduct.ImagePath);
                        }

                        // Upload new image
                        var uploadResult = await _fileUploadService.UploadFileAsync(productImage, "products");
                        product.ImagePath = uploadResult;
                        product.ImageFileName = productImage.FileName;
                        _logger.LogInformation("Product image uploaded successfully: {ImagePath}", uploadResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading product image");
                        ModelState.AddModelError("productImage", "Failed to upload image. Please try again.");
                        model.Categories = GetCategories().Select(c => c.Text).ToList();
                        return View(model);
                    }
                }
                else
                {
                    // Preserve existing image data if no new image is uploaded
                    product.ImagePath = existingProduct.ImagePath;
                    product.ImageFileName = existingProduct.ImageFileName;
                }

                // Validate product date is not in the future
                if (model.ProductDate > DateTime.Now)
                {
                    ModelState.AddModelError("ProductDate", "Production date cannot be in the future.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(product);
                        await _context.SaveChangesAsync();
                        
                        // Invalidate product cache
                        _cachingService.Remove(CacheKeys.ProductsList);
                        _cachingService.Remove(string.Format(CacheKeys.ProductsListByCategory, userId));
                        _cachingService.RemoveByPrefix("products_filtered_");
                        _cachingService.Remove(CacheKeys.DashboardAnalytics);
                        _cachingService.Remove(CacheKeys.ProductTrends);
                        _cachingService.RemoveByPrefix(CacheKeys.ProductAnalytics.Replace("{0}", ""));
                        
                        // Create notification for successful product update
                        await _notificationService.CreateNotificationAsync(
                            userId,
                            "Product Updated",
                            $"Your product '{product.Name}' has been successfully updated.",
                            NotificationType.Info
                        );
                        
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

            model.Categories = GetCategories().Select(c => c.Text).ToList();
            return View(model);
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

                // Delete associated image file if it exists
                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    try
                    {
                        await _fileUploadService.DeleteFileAsync(product.ImagePath);
                        _logger.LogInformation("Product image deleted: {ImagePath}", product.ImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete product image: {ImagePath}", product.ImagePath);
                        // Continue with product deletion even if image deletion fails
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                
                // Create notification for successful product deletion
                await _notificationService.CreateNotificationAsync(
                    userId,
                    "Product Deleted",
                    $"Your product '{product.Name}' has been successfully deleted.",
                    NotificationType.Warning
                );
                
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
