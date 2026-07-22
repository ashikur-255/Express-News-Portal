using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsApp.Data;
using NewsApp.Models;

namespace NewsApp.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================
        // 🌐 PUBLIC - CATEGORY LIST
        // =========================================
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(x => x.NameEnglish)
                .ToListAsync();

            return View(categories);
        }

        // =========================================
        // 🌐 PUBLIC - CATEGORY DETAILS
        // =========================================
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .AsNoTracking()
                .Include(x => x.News)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // =========================================
        // 🔐 ADMIN - CREATE (GET)
        // =========================================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // =========================================
        // 🔐 ADMIN - CREATE (POST)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            TempData["success"] = "Category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // 🔐 ADMIN - EDIT (GET)
        // =========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .FindAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // =========================================
        // 🔐 ADMIN - EDIT (POST)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(category);

            try
            {
                _context.Update(category);

                await _context.SaveChangesAsync();

                TempData["success"] = "Category updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // 🔐 ADMIN - DELETE (GET)
        // =========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // =========================================
        // 🔐 ADMIN - DELETE (POST)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(x => x.News)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
                return NotFound();

            // ✅ Prevent delete if category has news
            if (category.News != null && category.News.Any())
            {
                TempData["error"] =
                    "Cannot delete category because news exists under this category.";

                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            TempData["success"] = "Category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // HELPER
        // =========================================
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(x => x.Id == id);
        }
    }
}