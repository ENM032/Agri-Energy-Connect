using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication2.Areas.Identity.Data;

namespace WebApplication2.Models
{
    public class Product
    {
        /* This code was inspired by an online blog 
         * Titled: Introduction to relationships
         * Uploaded by: Microsoft
         * Availble at: https://learn.microsoft.com/en-us/ef/core/modeling/relationships
         * Accessed 26 May 2024
        */
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_]+$", ErrorMessage = "Product name contains invalid characters")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category must not exceed 50 characters")]
        public required string Category { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Production Date")]
        public DateTime ProductDate { get; set; }

        [Required]
        public string UserId { get; set; }

        [ValidateNever]
        public WebApplication2User User { get; set; }
    }
}
