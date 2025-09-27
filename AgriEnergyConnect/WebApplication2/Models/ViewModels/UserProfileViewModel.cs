using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// View model for displaying user profile information
    /// </summary>
    public class UserProfileViewModel
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
        
        public int TotalProducts { get; set; }
        
        public DateTime? LastProductDate { get; set; }
        
        public List<ProductViewModel> RecentProducts { get; set; } = new List<ProductViewModel>();
        
        public List<NotificationViewModel> RecentNotifications { get; set; } = new List<NotificationViewModel>();
        
        // Display properties
        public string PrimaryRole => Roles.FirstOrDefault() ?? "User";
        
        public string RoleDisplayName => PrimaryRole switch
        {
            "Admin" => "Administrator",
            "SupportEmployee" => "Support Employee",
            "Farmer" => "Farmer",
            _ => "User"
        };
        
        public string FormattedCreatedDate => CreatedDate.ToString("MMM dd, yyyy");
        
        public string FormattedLastLoginDate => LastLoginDate?.ToString("MMM dd, yyyy HH:mm") ?? "Never";
        
        public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow;
        
        public string AccountStatus => IsLocked ? "Locked" : EmailConfirmed ? "Active" : "Pending Verification";
        
        public string AccountStatusClass => AccountStatus switch
        {
            "Active" => "text-success",
            "Locked" => "text-danger",
            "Pending Verification" => "text-warning",
            _ => "text-muted"
        };
        
        // Additional properties for AutoMapper compatibility
        public bool IsActive => !IsLocked && EmailConfirmed;
        public int ProductCount => TotalProducts;
        public int NotificationCount => RecentNotifications.Count;
        public string FormattedLastLogin => FormattedLastLoginDate;
        
        // Permission properties
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
    
    /// <summary>
    /// View model for editing user profile
    /// </summary>
    public class EditUserProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        public bool EmailConfirmed { get; set; }
        
        public List<string> CurrentRoles { get; set; } = new List<string>();
        
        public string PrimaryRole => CurrentRoles.FirstOrDefault() ?? "User";
    }
    
    /// <summary>
    /// View model for changing password
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// View model for user management (Admin)
    /// </summary>
    public class UserManagementViewModel
    {
        public List<UserProfileViewModel> Users { get; set; } = new List<UserProfileViewModel>();
        
        public string? SearchTerm { get; set; }
        
        public string? RoleFilter { get; set; }
        
        public string? StatusFilter { get; set; }
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public int CurrentPage { get; set; } = 1;
        
        public int TotalPages { get; set; }
        
        public int TotalUsers { get; set; }
        
        public int PageSize { get; set; } = 20;
        
        public List<string> AvailableRoles { get; set; } = new List<string>
        {
            "Admin",
            "SupportEmployee",
            "Farmer"
        };
        
        public List<string> AvailableStatuses { get; set; } = new List<string>
        {
            "Active",
            "Locked",
            "Pending Verification"
        };
        
        public bool HasFilters => !string.IsNullOrEmpty(SearchTerm) || 
                                 !string.IsNullOrEmpty(RoleFilter) || 
                                 !string.IsNullOrEmpty(StatusFilter) || 
                                 StartDate.HasValue || 
                                 EndDate.HasValue;
    }
    
    /// <summary>
    /// View model for creating new users (Admin)
    /// </summary>
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
        
        public List<string> AvailableRoles { get; set; } = new List<string>
        {
            "Admin",
            "SupportEmployee",
            "Farmer"
        };
        
        public bool SendWelcomeEmail { get; set; } = true;
    }
}