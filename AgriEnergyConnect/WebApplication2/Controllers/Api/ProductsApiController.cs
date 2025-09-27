using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Controllers.Api
{
    /// <summary>
    /// REST API controller for Products - provides JSON endpoints for mobile apps and external integrations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ProductsApiController : ControllerBase
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly ILogger<ProductsApiController> _logger;

        public ProductsApiController(
            WebApplication2Context context,
            UserManager<WebApplication2User> userManager,
            ILogger<ProductsApiController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all products with optional filtering
        /// </summary>
        /// <param name="category">Filter by category</param>
        /// <param name="userId">Filter by user ID (Admin/Support only)</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated list of products</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts(
            [FromQuery] string? category = null,
            [FromQuery] string? userId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<PagedResult<ProductDto>>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isAdminOrSupport = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");

                var query = _context.Products.Include(p => p.User).AsQueryable();

                // Apply user filtering based on role
                if (!isAdminOrSupport)
                {
                    // Farmers can only see their own products
                    query = query.Where(p => p.UserId == currentUserId);
                }
                else if (!string.IsNullOrEmpty(userId))
                {
                    // Admin/Support can filter by specific user
                    query = query.Where(p => p.UserId == userId);
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                var totalCount = await query.CountAsync();
                var products = await query
                    .OrderByDescending(p => p.ProductDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Category = p.Category,
                        ProductDate = p.ProductDate,
                        UserId = p.UserId,
                        UserName = p.User != null ? p.User.UserName ?? "Unknown" : "Unknown",
                         UserDisplayName = p.User != null ? p.User.Displayname ?? "Unknown" : "Unknown"
                    })
                    .ToListAsync();

                var result = new PagedResult<ProductDto>
                {
                    Items = products,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(new ApiResponse<PagedResult<ProductDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Products retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new ApiResponse<PagedResult<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving products"
                });
            }
        }

        /// <summary>
        /// Get a specific product by ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isAdminOrSupport = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");

                var product = await _context.Products
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                // Check authorization
                if (!isAdminOrSupport && product.UserId != currentUserId)
                {
                    return Forbid();
                }

                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Category = product.Category,
                    ProductDate = product.ProductDate,
                    UserId = product.UserId,
                    UserName = product.User?.UserName ?? "Unknown",
                    UserDisplayName = product.User?.Displayname ?? "Unknown"
                };

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = productDto,
                    Message = "Product retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the product"
                });
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        /// <param name="createProductDto">Product creation data</param>
        /// <returns>Created product</returns>
        [HttpPost]
        [Authorize(Roles = "Farmer,Admin,Support Employee")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Invalid product data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var currentUserId = _userManager.GetUserId(User);
                if (currentUserId == null)
                {
                    return Unauthorized(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }
                var product = new Product
                {
                    Name = createProductDto.Name,
                    Category = createProductDto.Category,
                    ProductDate = createProductDto.ProductDate,
                    UserId = currentUserId
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Reload with user data
                await _context.Entry(product)
                    .Reference(p => p.User)
                    .LoadAsync();

                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Category = product.Category,
                    ProductDate = product.ProductDate,
                    UserId = product.UserId,
                    UserName = product.User?.UserName ?? "Unknown",
                    UserDisplayName = product.User?.Displayname ?? "Unknown"
                };

                _logger.LogInformation("Product {ProductName} created successfully by user {UserId}", product.Name, currentUserId);

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = productDto,
                    Message = "Product created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the product"
                });
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="updateProductDto">Product update data</param>
        /// <returns>Updated product</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Invalid product data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isAdminOrSupport = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");

                var product = await _context.Products
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                // Check authorization
                if (!isAdminOrSupport && product.UserId != currentUserId)
                {
                    return Forbid();
                }

                // Update product properties
                product.Name = updateProductDto.Name;
                product.Category = updateProductDto.Category;
                product.ProductDate = updateProductDto.ProductDate;

                await _context.SaveChangesAsync();

                var productDto = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Category = product.Category,
                    ProductDate = product.ProductDate,
                    UserId = product.UserId,
                    UserName = product.User?.UserName ?? "Unknown",
                    UserDisplayName = product.User?.Displayname ?? "Unknown"
                };

                _logger.LogInformation("Product {ProductId} updated successfully by user {UserId}", id, currentUserId);

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = productDto,
                    Message = "Product updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the product"
                });
            }
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Deletion confirmation</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                var isAdminOrSupport = userRoles.Contains("Admin") || userRoles.Contains("Support Employee");

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                // Check authorization
                if (!isAdminOrSupport && product.UserId != currentUserId)
                {
                    return Forbid();
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product {ProductId} deleted successfully by user {UserId}", id, currentUserId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Product deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the product"
                });
            }
        }

        /// <summary>
        /// Get available product categories
        /// </summary>
        /// <returns>List of categories</returns>
        [HttpGet("categories")]
        public ActionResult<ApiResponse<List<string>>> GetCategories()
        {
            try
            {
                var categories = new List<string>
                {
                    "Fruits",
                    "Vegetables",
                    "Grains",
                    "Dairy",
                    "Livestock",
                    "Herbs",
                    "Nuts",
                    "Other"
                };

                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Data = categories,
                    Message = "Categories retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving categories"
                });
            }
        }
    }
}