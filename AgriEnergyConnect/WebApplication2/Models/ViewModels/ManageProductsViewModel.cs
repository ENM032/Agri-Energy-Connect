using System.Collections.Generic;
using WebApplication2.Models;

namespace WebApplication2.Models.ViewModels
{
    public class ManageProductsViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public int TotalProducts { get; set; }
        public int ProductsCreatedToday { get; set; }
        public int ProductsCreatedThisWeek { get; set; }
    }
}