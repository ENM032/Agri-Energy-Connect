using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Data;
using System.Net;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    // Only accessible to employees
    [Authorize(Roles = "Employee")]
    public class EmployeesController : Controller
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeesController(WebApplication2Context context, UserManager<WebApplication2User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            this._userManager = userManager;
            this._roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await getAllFarmersFromDb().ToListAsync());
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FarmerRegistrationModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new WebApplication2User { UserName = model.Email, Email = model.Email, Displayname = model.DisplayName };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(String.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> DeleteFarmer(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(p => p.Products)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }            
            return View(user);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("DeleteFarmer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFarmerConfirmed(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Products/Delete/5
        public async Task<IActionResult> DeleteFarmerProduct(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("DeleteFarmerProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFarmerProductConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(FarmerProducts));
        }

        // Displays a view of all the farmer products
        public async Task<IActionResult> FarmerProducts()
        {
            //ViewData["UserId"] = new SelectList(getAllFarmersFromDb(), "Id", "Id");
            ViewData["UserName"] = new SelectList(getAllFarmersFromDb(), "UserName", "UserName");
            ViewBag.CategoriesSelectList = new SelectList(ProductsController.GetCategories(), "Id");
            var webApplication2Context = _context.Products.Include(p => p.User);
            displayDbRequestError(webApplication2Context, "No products available to display. Farmers still need to add their products");
            return View(await webApplication2Context.ToListAsync());
        }

        // Filters the farmer products when the employee clicks on the filter button
        [HttpPost]
        public async Task<IActionResult> FarmerProducts(string selectedUser, string selectedCategory, DateTime betweenStartDate, DateTime betweenEndDate)
        {
            ViewData["UserName"] = new SelectList(getAllFarmersFromDb(), "UserName", "UserName");
            ViewBag.CategoriesSelectList = new SelectList(ProductsController.GetCategories(), "Id");
            var webApplication2Context = _context.Products.Include(p => p.User);

            if (selectedUser != null && selectedCategory == null && betweenEndDate == DateTime.MinValue && betweenEndDate == DateTime.MinValue)
            {
                webApplication2Context = _context.Products.Where(x => x.User.UserName == selectedUser).Include(p => p.User);
                displayDbRequestError(webApplication2Context, "No products associated with this user available");
            }
            if(selectedUser == null && selectedCategory != null && betweenEndDate == DateTime.MinValue && betweenEndDate == DateTime.MinValue)
            {
                webApplication2Context = _context.Products.Where(x => x.Category == selectedCategory).Include(p => p.User);
                displayDbRequestError(webApplication2Context, "No products associated with this category available");
            }
            /*
             * This code to check the value of dateTime component was taken from a Stack overflow post
             * Uploaded by: Fabian Bigler
             * Titled: How to check if a DateTime field is not null or empty? [duplicate]
             * Available at: https://stackoverflow.com/questions/21905733/how-to-check-if-a-datetime-field-is-not-null-or-empty
             * Accessed 24 May 2023
            */
            if (selectedUser == null && selectedCategory == null && betweenEndDate != DateTime.MinValue && betweenEndDate != DateTime.MinValue)
            {
                webApplication2Context = _context.Products.Where(x => x.ProductDate >= betweenStartDate && x.ProductDate <= betweenEndDate).Include(p => p.User);
                displayDbRequestError(webApplication2Context, "No products associated with this date range available");
            }

            if (selectedUser != null && selectedCategory != null && betweenEndDate == DateTime.MinValue && betweenEndDate == DateTime.MinValue)
            {
                webApplication2Context = _context.Products.Where(x => x.User.UserName == selectedUser
                    && x.Category == selectedCategory)
                        .Include(p => p.User);
                displayDbRequestError(webApplication2Context, "No products associated with this user and category available");
            }
            if (selectedUser != null && selectedCategory == null && betweenEndDate != DateTime.MinValue && betweenEndDate != DateTime.MinValue)
            {
                webApplication2Context = _context.Products.Where(x => x.User.UserName == selectedUser
                     && x.ProductDate >= betweenStartDate
                            && x.ProductDate <= betweenEndDate)
                                .Include(p => p.User);
                displayDbRequestError(webApplication2Context, "No products associated with this user and date range available");
            }
            if (selectedUser == null && selectedCategory != null && betweenEndDate != DateTime.MinValue && betweenEndDate != DateTime.MinValue)
            {
                webApplication2Context = _context.Products.Where(x => x.Category == selectedCategory
                     && x.ProductDate >= betweenStartDate
                            && x.ProductDate <= betweenEndDate)
                                .Include(p => p.User);
                displayDbRequestError(webApplication2Context, "No products associated with this category and date range available");
            }
            if (selectedUser != null && selectedCategory != null && betweenEndDate != DateTime.MinValue && betweenEndDate != DateTime.MinValue)
            {
                webApplication2Context = _context.Products.Where(x => x.User.UserName == selectedUser
                    && x.Category == selectedCategory
                        && x.ProductDate >= betweenStartDate
                            && x.ProductDate <= betweenEndDate)
                                .Include(p => p.User);
                displayDbRequestError(webApplication2Context, "No products associated with these 4 filter parameters available");
            }

            return View(await webApplication2Context.ToListAsync());
        }

        // Retrieves all farmers
        public IQueryable<WebApplication2User> getAllFarmersFromDb()
        {
            /*
             * The code for joining tables was taken from a Stack overflow post
             * Titled: What is the proper way to Join two tables in ASP.NET MVC?
             * Posted by: HaBo
             * Available at: https://stackoverflow.com/questions/26852219/what-is-the-proper-way-to-join-two-tables-in-asp-net-mvc
             * Accessed 28 April 2024
            */           
            var farmers = (from userRole in _context.UserRoles
                                          join user in _context.Users on userRole.UserId equals user.Id
                                          join role in _context.Roles on userRole.RoleId equals role.Id
                                          where role.Name == "Farmer"
                                          select user);

            if(farmers.IsNullOrEmpty())
            {
                ModelState.AddModelError(String.Empty, "No farmer profiles found");
            }

            return farmers;
        }

        public void displayDbRequestError(IIncludableQueryable<Product, WebApplication2User> context, string message)
        {
            if(context.IsNullOrEmpty())
            {
                ModelState.AddModelError(String.Empty, message);
            }
        }

    }
}
