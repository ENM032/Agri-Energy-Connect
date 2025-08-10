using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using WebApplication2.Areas.Identity.Data;

namespace WebApplication2.Services
{
    /// <summary>
    /// Service for handling authorization logic with caching to reduce database calls
    /// </summary>
    public class AuthorizationHelperService : IAuthorizationHelperService
    {
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthorizationHelperService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public AuthorizationHelperService(
            UserManager<WebApplication2User> userManager,
            IMemoryCache cache,
            ILogger<AuthorizationHelperService> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CanUserEditProductAsync(ClaimsPrincipal user, string productUserId)
        {
            try
            {
                var currentUserId = GetCurrentUserId(user);
                if (string.IsNullOrEmpty(currentUserId))
                    return false;

                // User can edit their own products
                if (currentUserId == productUserId)
                    return true;

                // Admin and Support Employee can edit any product
                var userRoles = await GetUserRolesAsync(user);
                return userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking edit permissions for user {UserId} on product owned by {ProductUserId}", 
                    GetCurrentUserId(user), productUserId);
                return false;
            }
        }

        public async Task<bool> CanUserDeleteProductAsync(ClaimsPrincipal user, string productUserId)
        {
            try
            {
                var currentUserId = GetCurrentUserId(user);
                if (string.IsNullOrEmpty(currentUserId))
                    return false;

                // User can delete their own products
                if (currentUserId == productUserId)
                    return true;

                // Admin and Support Employee can delete any product
                var userRoles = await GetUserRolesAsync(user);
                return userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delete permissions for user {UserId} on product owned by {ProductUserId}", 
                    GetCurrentUserId(user), productUserId);
                return false;
            }
        }

        public async Task<bool> IsUserInRoleAsync(ClaimsPrincipal user, string role)
        {
            try
            {
                var userRoles = await GetUserRolesAsync(user);
                return userRoles.Contains(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is in role {Role}", role);
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(ClaimsPrincipal user)
        {
            try
            {
                var userId = GetCurrentUserId(user);
                if (string.IsNullOrEmpty(userId))
                    return new List<string>();

                var cacheKey = $"user_roles_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out List<string>? cachedRoles) && cachedRoles != null)
                {
                    return cachedRoles;
                }

                var currentUser = await _userManager.GetUserAsync(user);
                if (currentUser == null)
                    return new List<string>();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var rolesList = roles.ToList();

                // Cache the roles
                _cache.Set(cacheKey, rolesList, _cacheExpiration);
                
                return rolesList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user roles for user {UserId}", GetCurrentUserId(user));
                return new List<string>();
            }
        }

        public async Task<bool> CanViewAllProductsAsync(ClaimsPrincipal user)
        {
            try
            {
                var userRoles = await GetUserRolesAsync(user);
                return userRoles.Contains("Admin") || userRoles.Contains("Support Employee");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking view all products permission for user {UserId}", GetCurrentUserId(user));
                return false;
            }
        }

        public string? GetCurrentUserId(ClaimsPrincipal user)
        {
            try
            {
                return _userManager.GetUserId(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user ID");
                return null;
            }
        }
    }
}