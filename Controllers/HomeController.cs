
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsApp.Data;
using NewsApp.Models;
using NewsApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext context)
        {
            _db = context;
        }

        // =========================================================
        // 🏠 HOME PAGE
        // =========================================================
        public async Task<IActionResult> Index(int page = 1, int pageSize = 16)
        {
            page = page < 1 ? 1 : page;

            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToListAsync();

            var totalNews = await _db.News.CountAsync();

            var news = await _db.News
                .Include(x => x.Category)
                .AsNoTracking()
                .OrderByDescending(x => x.IsBreaking)
                .ThenByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new HomeViewModel
            {
                Categories = categories ?? new List<Category>(),
                News = news ?? new List<News>(),

                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalNews,
                TotalPages = (int)Math.Ceiling(totalNews / (double)pageSize)
            };

            return View(vm);
        }

        // =========================================================
        // 🌐 LANGUAGE SWITCHER
        // =========================================================
        public IActionResult SetLanguage(
            string culture,
            string returnUrl = "/")
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,

                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture)),

                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });

            return LocalRedirect(returnUrl);
        }

        // =========================================================
        // 📰 NEWS BY CATEGORY
        // =========================================================
        public async Task<IActionResult> News(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
                return NotFound();

            ViewBag.Category = category;

            var news = await _db.News
                .Include(n => n.Category)
                .Where(x => x.CategoryId == id)
                .AsNoTracking()
                .OrderByDescending(x => x.IsBreaking)
                .ThenByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return View(news);
        }

        // =========================================================
        // 📄 STATIC PAGES
        // =========================================================
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // =========================================================
        // 📩 SAVE CONTACT MESSAGE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveContact(ContentUs model)
        {
            if (!ModelState.IsValid)
            {
                return View("Contact", model);
            }

            model.date = DateTime.Now;

            await _db.Contents.AddAsync(model);

            await _db.SaveChangesAsync();

            TempData["success"] =
                "Message sent successfully!";

            return RedirectToAction(nameof(Contact));
        }

        // =========================================================
        // 📬 ADMIN MESSAGES
        // =========================================================
        public async Task<IActionResult> Messages()
        {
            var messages = await _db.Contents
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(messages);
        }

        // =========================================================
        // 🗑 DELETE PAGE
        // =========================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await _db.News
                .Include(x => x.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // =========================================================
        // 🗑 DELETE CONFIRMED
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _db.News
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
                return NotFound();

            _db.News.Remove(item);

            await _db.SaveChangesAsync();

            TempData["success"] =
                "News deleted successfully!";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ⚠ ERROR PAGE
        // =========================================================
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = HttpContext.TraceIdentifier
                });
        }
    }
}

