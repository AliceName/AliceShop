using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TranThiNga_2380601420_Tuan4_LTWeb.Models;
using TranThiNga_2380601420_Tuan4_LTWeb.Repositories;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _env;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment env)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _env = env;
        }

        // ── Index ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // ── Add (GET) ──────────────────────────────────────────────────────
        public async Task<IActionResult> Add()
        {
            await LoadCategoriesAsync();
            return View();
        }

        // ── Add (POST) ─────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, List<IFormFile> imageFiles)
        {
            if (ModelState.IsValid)
            {
                if (imageFiles != null && imageFiles.Count > 0)
                {
                    product.Images = new List<ProductImage>();
                    foreach (var file in imageFiles.Where(f => f.Length > 0))
                    {
                        var savedPath = await SaveImage(file);
                        if (string.IsNullOrEmpty(product.ImageUrl))
                            product.ImageUrl = savedPath;          // ảnh đầu tiên = ảnh đại diện
                        product.Images.Add(new ProductImage { Url = savedPath });
                    }
                }

                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesAsync();
            return View(product);
        }

        // ── Display ────────────────────────────────────────────────────────
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ── Update (GET) ───────────────────────────────────────────────────
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            await LoadCategoriesAsync(product.CategoryId);
            return View(product);
        }

        // ── Update (POST) ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Product product, List<IFormFile> imageFiles, List<int>? removedImageIds)
        {
            ModelState.Remove("ImageUrl");
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _productRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                // Cập nhật thông tin cơ bản
                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.Description = product.Description;
                existing.CategoryId = product.CategoryId;
                // KHÔNG gán existing.ImageUrl = product.ImageUrl
                // vì product.ImageUrl từ form luôn null

                if (removedImageIds != null && removedImageIds.Count > 0 && existing.Images != null)
                {
                    var imagesToRemove = existing.Images.Where(img => removedImageIds.Contains(img.Id)).ToList();
                    foreach (var img in imagesToRemove)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                        existing.Images.Remove(img);
                    }

                    if (imagesToRemove.Any(img => img.Url == existing.ImageUrl))
                    {
                        existing.ImageUrl = existing.Images.FirstOrDefault()?.Url;
                    }
                }

                if (imageFiles != null && imageFiles.Count > 0)
                {
                    // Có ảnh mới → thêm vào danh sách và cập nhật ảnh đại diện
                    existing.Images ??= new List<ProductImage>();
                    foreach (var file in imageFiles.Where(f => f.Length > 0))
                    {
                        var savedPath = await SaveImage(file);
                        existing.ImageUrl = savedPath;             // ảnh cuối cùng upload = ảnh đại diện mới
                        existing.Images.Add(new ProductImage { Url = savedPath });
                    }
                }
                // Không có ảnh mới → giữ nguyên existing.ImageUrl cũ

                await _productRepository.UpdateAsync(existing);
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesAsync(product.CategoryId);
            return View(product);
        }

        // ── Delete (GET) ───────────────────────────────────────────────────
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ── Delete (POST) ──────────────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private async Task<string> SaveImage(IFormFile image)
        {
            var ext = Path.GetExtension(image.FileName);
            var fileName = Guid.NewGuid().ToString("N") + ext;   // tên duy nhất, tránh trùng
            var folder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(folder);                    // tạo nếu chưa có

            var fullPath = Path.Combine(folder, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await image.CopyToAsync(stream);

            return "/images/" + fileName;
        }

        private async Task LoadCategoriesAsync(int? selectedId = null)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
        }
    }
}