using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// View model for displaying product information
    /// </summary>
    public class ProductViewModel
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        
        public DateTime ProductDate { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        public string UserDisplayName { get; set; } = string.Empty;
        
        public string? ImagePath { get; set; }
        
        public string? ImageFileName { get; set; }
        
        public string ImageUrl => !string.IsNullOrEmpty(ImagePath) ? $"/uploads/{ImagePath}" : "/images/no-image.png";
        
        public bool HasImage => !string.IsNullOrEmpty(ImagePath);
        
        public string FormattedProductDate => ProductDate.ToString("MMM dd, yyyy");
        
        public string FormattedDate => ProductDate.ToString("MMM dd, yyyy");
        
        public string CategoryDisplayName => Category?.Replace("_", " ") ?? "Unknown";
        
        public string CategoryName => Category?.Replace("_", " ") ?? "Unknown";
        
        public bool IsOwner { get; set; }
        
        public bool CanEdit { get; set; }
        
        public bool CanDelete { get; set; }
    }
    
    /// <summary>
    /// View model for creating new products
    /// </summary>
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Product date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Product Date")]
        [FutureDate(ErrorMessage = "Product date cannot be in the past")]
        public DateTime ProductDate { get; set; } = DateTime.Today.AddDays(1);
        
        [Display(Name = "Product Image")]
        [DataType(DataType.Upload)]
        public IFormFile? ProductImage { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public List<string> AvailableCategories { get; set; } = new List<string>
        {
            "Vegetables",
            "Fruits",
            "Grains",
            "Dairy",
            "Meat",
            "Herbs",
            "Other"
        };
        
        public List<string> Categories { get; set; } = new List<string>();
        
        public string ImageUploadHelpText => "Supported formats: JPG, JPEG, PNG, GIF, BMP, WEBP. Maximum size: 5MB.";
    }
    
    /// <summary>
    /// View model for editing existing products
    /// </summary>
    public class ProductEditViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Product date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Product Date")]
        [FutureDate(ErrorMessage = "Product date cannot be in the past")]
        public DateTime ProductDate { get; set; }
        
        [Display(Name = "New Product Image")]
        [DataType(DataType.Upload)]
        public IFormFile? ProductImage { get; set; }
        
        public string? CurrentImagePath { get; set; }
        
        public string? CurrentImageFileName { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public List<string> AvailableCategories { get; set; } = new List<string>
        {
            "Vegetables",
            "Fruits",
            "Grains",
            "Dairy",
            "Meat",
            "Herbs",
            "Other"
        };
        
        public List<string> Categories { get; set; } = new List<string>();
        
        public bool RemoveCurrentImage { get; set; }
        
        public string CurrentImageUrl => !string.IsNullOrEmpty(CurrentImagePath) ? $"/uploads/{CurrentImagePath}" : "/images/no-image.png";
        
        public bool HasCurrentImage => !string.IsNullOrEmpty(CurrentImagePath);
        
        public string ImageUploadHelpText => "Supported formats: JPG, JPEG, PNG, GIF, BMP, WEBP. Maximum size: 5MB. Leave empty to keep current image.";
    }
    
    /// <summary>
    /// View model for product listing with filtering
    /// </summary>
    public class ProductIndexViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        
        public string? SearchTerm { get; set; }
        
        public string? SearchName { get; set; }
        
        public string? CategoryFilter { get; set; }
        
        public string? UserFilter { get; set; }
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public DateTime? DateFrom { get; set; }
        
        public DateTime? DateTo { get; set; }
        
        public int CurrentPage { get; set; } = 1;
        
        public int TotalPages { get; set; }
        
        public int TotalProducts { get; set; }
        
        public int PageSize { get; set; } = 12;
        
        public List<string> AvailableCategories { get; set; } = new List<string>();
        
        public List<string> Categories { get; set; } = new List<string>();
        
        public List<UserDto> AvailableUsers { get; set; } = new List<UserDto>();
        
        public bool CanViewAllProducts { get; set; }
        
        public bool HasFilters => !string.IsNullOrEmpty(SearchTerm) || 
                                 !string.IsNullOrEmpty(CategoryFilter) || 
                                 !string.IsNullOrEmpty(UserFilter) || 
                                 StartDate.HasValue || 
                                 EndDate.HasValue;
        
        public string ViewMode { get; set; } = "grid"; // "grid" or "table"
    }
}