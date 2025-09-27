using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for User responses
    /// </summary>
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string DisplayName { get; set; } = string.Empty;
        
        public List<string> Roles { get; set; } = new List<string>();
        
        public bool EmailConfirmed { get; set; }
        
        public bool LockoutEnabled { get; set; }
        
        public DateTimeOffset? LockoutEnd { get; set; }
        
        public DateTime? LastLoginDate { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for updating user profile
    /// </summary>
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string DisplayName { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Data Transfer Object for creating new users (Admin only)
    /// </summary>
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string UserName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Data Transfer Object for user login
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        public bool RememberMe { get; set; } = false;
    }
    
    /// <summary>
    /// Data Transfer Object for user registration
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string UserName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Data Transfer Object for password change
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}