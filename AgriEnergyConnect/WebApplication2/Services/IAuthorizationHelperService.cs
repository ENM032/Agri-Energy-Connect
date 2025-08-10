using System.Security.Claims;

namespace WebApplication2.Services
{
    /// <summary>
    /// Service interface for handling authorization logic and reducing code duplication
    /// </summary>
    public interface IAuthorizationHelperService
    {
        /// <summary>
        /// Check if the current user can edit a specific product
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <param name="productUserId">User ID of the product owner</param>
        /// <returns>True if user can edit the product</returns>
        Task<bool> CanUserEditProductAsync(ClaimsPrincipal user, string productUserId);

        /// <summary>
        /// Check if the current user can delete a specific product
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <param name="productUserId">User ID of the product owner</param>
        /// <returns>True if user can delete the product</returns>
        Task<bool> CanUserDeleteProductAsync(ClaimsPrincipal user, string productUserId);

        /// <summary>
        /// Check if user is in a specific role with caching
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <param name="role">Role name to check</param>
        /// <returns>True if user is in the specified role</returns>
        Task<bool> IsUserInRoleAsync(ClaimsPrincipal user, string role);

        /// <summary>
        /// Get all roles for a user with caching
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <returns>List of role names</returns>
        Task<List<string>> GetUserRolesAsync(ClaimsPrincipal user);

        /// <summary>
        /// Check if user can view all products (Admin or Support Employee)
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <returns>True if user can view all products</returns>
        Task<bool> CanViewAllProductsAsync(ClaimsPrincipal user);

        /// <summary>
        /// Get current user ID safely
        /// </summary>
        /// <param name="user">Current user claims principal</param>
        /// <returns>User ID or null if not found</returns>
        string? GetCurrentUserId(ClaimsPrincipal user);
    }
}