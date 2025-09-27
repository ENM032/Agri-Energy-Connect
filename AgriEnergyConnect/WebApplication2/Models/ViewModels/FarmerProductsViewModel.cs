using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// View model for displaying farmer products with filtering and pagination
    /// </summary>
    public class FarmerProductsViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        
        // Filter properties
        [Display(Name = "Select Farmer")]
        public string? SelectedUser { get; set; }
        
        [Display(Name = "Select Category")]
        public string? SelectedCategory { get; set; }
        
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? BetweenStartDate { get; set; }
        
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? BetweenEndDate { get; set; }
        
        // Dropdown lists
        public SelectList? UserNameList { get; set; }
        public SelectList? CategoriesSelectList { get; set; }
        
        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
        
        // Helper properties
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasProducts => Products.Any();
        public bool HasFilters => !string.IsNullOrEmpty(SelectedUser) || 
                                 !string.IsNullOrEmpty(SelectedCategory) || 
                                 BetweenStartDate.HasValue || 
                                 BetweenEndDate.HasValue;
    }
}