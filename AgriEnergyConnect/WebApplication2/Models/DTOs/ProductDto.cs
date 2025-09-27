using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Product responses
    /// </summary>
    public class ProductDto
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime ProductDate { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        public string UserDisplayName { get; set; } = string.Empty;
        
        // Additional properties for AutoMapper compatibility
        public string? ImagePath { get; set; }
        public string? ImageFileName { get; set; }
        public string CategoryName => Category;
        public bool HasImage => !string.IsNullOrEmpty(ImagePath);
        public string? ImageUrl => !string.IsNullOrEmpty(ImagePath) ? $"/uploads/products/{ImagePath}" : null;
    }
    
    /// <summary>
    /// Data Transfer Object for Product API responses
    /// </summary>
    public class ProductResponseDto : ProductDto
    {
        // Inherits all properties from ProductDto
        // Can add additional response-specific properties here if needed
    }
    
    /// <summary>
    /// Data Transfer Object for creating new products
    /// </summary>
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Product date is required")]
        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Product date cannot be in the past")]
        public DateTime ProductDate { get; set; }
    }
    
    /// <summary>
    /// Data Transfer Object for updating existing products
    /// </summary>
    public class UpdateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Product date is required")]
        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Product date cannot be in the past")]
        public DateTime ProductDate { get; set; }
    }
    
    /// <summary>
    /// Custom validation attribute to ensure date is in the future
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
            {
                return date.Date >= DateTime.Today;
            }
            return false;
        }
    }
}