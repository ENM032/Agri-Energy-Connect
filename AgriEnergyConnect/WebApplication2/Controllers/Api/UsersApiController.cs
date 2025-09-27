using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Controllers.Api
{
    /// <summary>
    /// REST API controller for User management - provides JSON endpoints for user operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UsersApiController : ControllerBase
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UsersApiController> _logger;

        public UsersApiController(
            WebApplication2Context context,
            UserManager<WebApplication2User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UsersApiController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all users (Admin and Support Employee only)
        /// </summary>
        /// <param name="role">Filter by role</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Support Employee")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
            [FromQuery] string? role = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var users = _userManager.Users.AsQueryable();

                // Apply role filter if specified
                if (!string.IsNullOrEmpty(role))
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    var userIds = usersInRole.Select(u => u.Id).ToList();
                    users = users.Where(u => userIds.Contains(u.Id));
                }

                var totalCount = await users.CountAsync();
                var userList = await users
                    .OrderBy(u => u.UserName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userDtos = new List<UserDto>();
                foreach (var user in userList)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        DisplayName = user.Displayname ?? string.Empty,
                        Roles = userRoles.ToList(),
                        EmailConfirmed = user.EmailConfirmed,
                        LockoutEnabled = user.LockoutEnabled,
                        LockoutEnd = user.LockoutEnd
                    });
                }

                var result = new PagedResult<UserDto>
                {
                    Items = userDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(new ApiResponse<PagedResult<UserDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Users retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new ApiResponse<PagedResult<UserDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving users"
                });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        /// <returns>Current user details</returns>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var userDto = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.Displayname ?? string.Empty,
                    Roles = userRoles.ToList(),
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd
                };

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = userDto,
                    Message = "Profile retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the profile"
                });
            }
        }

        /// <summary>
        /// Get a specific user by ID (Admin and Support Employee only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Support Employee")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var userDto = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.Displayname ?? string.Empty,
                    Roles = userRoles.ToList(),
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd
                };

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = userDto,
                    Message = "User retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the user"
                });
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        /// <param name="updateProfileDto">Profile update data</param>
        /// <returns>Updated profile</returns>
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Invalid profile data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Update user properties
                user.Displayname = updateProfileDto.DisplayName;
                
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Failed to update profile",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var userDto = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.Displayname ?? string.Empty,
                    Roles = userRoles.ToList(),
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd
                };

                _logger.LogInformation("Profile updated successfully for user {UserId}", user.Id);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = userDto,
                    Message = "Profile updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the profile"
                });
            }
        }

        /// <summary>
        /// Get farmers list (Admin and Support Employee only)
        /// </summary>
        /// <param name="page">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated list of farmers</returns>
        [HttpGet("farmers")]
        [Authorize(Roles = "Admin,Support Employee")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetFarmers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var farmers = await _userManager.GetUsersInRoleAsync("Farmer");
                var totalCount = farmers.Count;
                
                var pagedFarmers = farmers
                    .OrderBy(f => f.UserName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var farmerDtos = new List<UserDto>();
                foreach (var farmer in pagedFarmers)
                {
                    farmerDtos.Add(new UserDto
                    {
                        Id = farmer.Id,
                        UserName = farmer.UserName ?? string.Empty,
                        Email = farmer.Email ?? string.Empty,
                        DisplayName = farmer.Displayname ?? string.Empty,
                        Roles = new List<string> { "Farmer" },
                        EmailConfirmed = farmer.EmailConfirmed,
                        LockoutEnabled = farmer.LockoutEnabled,
                        LockoutEnd = farmer.LockoutEnd
                    });
                }

                var result = new PagedResult<UserDto>
                {
                    Items = farmerDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(new ApiResponse<PagedResult<UserDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Farmers retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving farmers");
                return StatusCode(500, new ApiResponse<PagedResult<UserDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving farmers"
                });
            }
        }

        /// <summary>
        /// Get available roles
        /// </summary>
        /// <returns>List of roles</returns>
        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                
                return Ok(new ApiResponse<List<string>>
                {
                    Success = true,
                    Data = roles.Where(r => r != null).Cast<string>().ToList(),
                    Message = "Roles retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new ApiResponse<List<string>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving roles"
                });
            }
        }
    }
}