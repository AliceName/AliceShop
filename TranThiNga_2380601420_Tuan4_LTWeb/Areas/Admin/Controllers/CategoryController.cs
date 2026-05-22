using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TranThiNga_2380601420_Tuan4_LTWeb.Models;
using TranThiNga_2380601420_Tuan4_LTWeb.Repositories;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _env;

        public CategoryController(ICategoryRepository categoryRepository, IProductRepository productRepository, IWebHostEnvironment env)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return View(categories);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepository.AddAsync(category);
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        public async Task<IActionResult> Display(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        public async Task<IActionResult> Update(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _categoryRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                existing.Name = category.Name;
                await _categoryRepository.UpdateAsync(existing);
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null) return NotFound();

            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();
            ViewBag.ProductCount = productsInCategory.Count;

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var allProducts = await _productRepository.GetAllAsync();
            var productsInCategory = allProducts.Where(p => p.CategoryId == id).ToList();

            foreach (var p in productsInCategory)
            {
                if (p.Images != null)
                {
                    foreach (var img in p.Images)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }
                await _productRepository.DeleteAsync(p.Id);
            }

            await _categoryRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }

}
