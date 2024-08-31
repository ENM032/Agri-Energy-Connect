using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    // Only shows you this action if youre a employee [Authorize(Roles = "Employee")]
    // Only shows you this page if youre a farmer [Authorize(Roles = "Farmer")]
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private string? userId;
        public ProductsController(WebApplication2Context context, UserManager<WebApplication2User> userManager)
        {
            this._userManager = userManager;
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            PopulateLocalUserIdVariable();
            var webApplication2Context = _context.Products.Where(x => x.UserId == userId).Include(p => p.User);
            if (webApplication2Context.IsNullOrEmpty())
            {
                ModelState.AddModelError(String.Empty, "No products available to display, please add products first");
            }
            return View(await webApplication2Context.ToListAsync());
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Id");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Category,ProductDate,UserId")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Id");
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Id");
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,ProductDate,UserId")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoriesSelectList = new SelectList(GetCategories(), "Id");
            //ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", product.UserId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private void PopulateLocalUserIdVariable()
        {
            userId = _userManager.GetUserId(this.User);
        }

        public static List<String> GetCategories()
        {
            var categories = new List<string>() { "Cereals", "Seeds", "Pulses", "Fruits", "Vegetables", "Herbs & Spices" };
            return categories;
        }
    }
}
