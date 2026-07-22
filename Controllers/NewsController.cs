using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewsApp.Data;
using NewsApp.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NewsApp.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NewsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =====================================================
        // PUBLIC LIST
        // =====================================================
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var news = await _context.News
                .Include(x => x.Category)
                .AsNoTracking()
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return View(news);
        }

        // =====================================================
        // DETAILS
        // =====================================================
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News
                .Include(x => x.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (news == null) return NotFound();

            return View(news);
        }

        // =====================================================
        // CREATE GET
        // =====================================================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            LoadCategories();

            return View(new News
            {
                Date = DateTime.Now
            });
        }

        // =====================================================
        // CREATE POST
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News news, IFormFile? ImageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    LoadCategories(news?.CategoryId);
                    return View(news ?? new News());
                }

                // DATE SAFE
                if (news.Date == default)
                {
                    news.Date = DateTime.Now;
                }

                // IMAGE UPLOAD
                news.Image = await UploadImageAsync(ImageFile);

                // VIDEO UPLOAD
                if (news.VideoFile != null && news.VideoFile.Length > 0)
                {
                    news.VideoPath = await UploadVideoAsync(news.VideoFile);
                }

                _context.News.Add(news);
                await _context.SaveChangesAsync();

                TempData["success"] = "News created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                LoadCategories(news?.CategoryId);
                return View(news ?? new News());
            }
        }

        // =====================================================
        // EDIT GET
        // =====================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News.FindAsync(id);

            if (news == null) return NotFound();

            LoadCategories(news.CategoryId);

            return View(news);
        }

        // =====================================================
        // EDIT POST
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, News news, IFormFile? ImageFile)
        {
            if (id != news.Id) return NotFound();

            var existing = await _context.News.FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null) return NotFound();

            try
            {
                if (!ModelState.IsValid)
                {
                    LoadCategories(news?.CategoryId);
                    return View(news);
                }

                // IMAGE UPDATE
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    DeleteImage(existing.Image);
                    existing.Image = await UploadImageAsync(ImageFile);
                }

                // VIDEO FILE UPDATE
                if (news.VideoFile != null && news.VideoFile.Length > 0)
                {
                    DeleteVideo(existing.VideoPath);
                    existing.VideoPath = await UploadVideoAsync(news.VideoFile);
                    existing.VideoUrl = null;
                }

                // YOUTUBE URL
                if (!string.IsNullOrWhiteSpace(news.VideoUrl))
                {
                    DeleteVideo(existing.VideoPath);
                    existing.VideoPath = null;
                    existing.VideoUrl = news.VideoUrl;
                }

                // UPDATE FIELDS
                existing.TitleEnglish = news.TitleEnglish;
                existing.TitleBangla = news.TitleBangla;
                existing.DescriptionEnglish = news.DescriptionEnglish;
                existing.DescriptionBangla = news.DescriptionBangla;
                existing.Date = news.Date;
                existing.CategoryId = news.CategoryId;
                existing.IsBreaking = news.IsBreaking;
                existing.IsFeatured = news.IsFeatured;

                await _context.SaveChangesAsync();

                TempData["success"] = "News updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                LoadCategories(news?.CategoryId);
                return View(news);
            }
        }

        // =====================================================
        // DELETE GET
        // =====================================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var news = await _context.News
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (news == null) return NotFound();

            return View(news);
        }

        // =====================================================
        // DELETE POST
        // =====================================================
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var news = await _context.News.FindAsync(id);

            if (news == null) return NotFound();

            DeleteImage(news.Image);
            DeleteVideo(news.VideoPath);

            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            TempData["success"] = "News deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // CATEGORY LOADER (FIXED)
        // =====================================================
        private void LoadCategories(object? selected = null)
        {
            var categories = _context.Categories
                .OrderBy(x => x.NameEnglish)
                .ToList();

            ViewBag.CategoryId = new SelectList(categories, "Id", "NameEnglish", selected);
        }

        // =====================================================
        // IMAGE UPLOAD
        // =====================================================
        private async Task<string> UploadImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "default.jpg";

            string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };

            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                throw new Exception("Invalid image format");

            string folder = Path.Combine(_env.WebRootPath, "assets/img");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid() + ext;

            string path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        // =====================================================
        // VIDEO UPLOAD
        // =====================================================
        private async Task<string> UploadVideoAsync(IFormFile file)
        {
            string[] allowed = { ".mp4", ".webm", ".ogg" };

            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                throw new Exception("Invalid video format");

            if (file.Length > 20 * 1024 * 1024)
                throw new Exception("Max video size is 20MB");

            string folder = Path.Combine(_env.WebRootPath, "assets/video");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid() + ext;

            string path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        // =====================================================
        // DELETE IMAGE
        // =====================================================
        private void DeleteImage(string? image)
        {
            if (string.IsNullOrWhiteSpace(image) || image == "default.jpg")
                return;

            string path = Path.Combine(_env.WebRootPath, "assets/img", image);

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        // =====================================================
        // DELETE VIDEO
        // =====================================================
        private void DeleteVideo(string? video)
        {
            if (string.IsNullOrWhiteSpace(video))
                return;

            string path = Path.Combine(_env.WebRootPath, "assets/video", video);

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}