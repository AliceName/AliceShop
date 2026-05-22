using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TranThiNga_2380601420_Tuan4_LTWeb.Repositories;
using TranThiNga_2380601420_Tuan4_LTWeb.Models;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index(string? q)
        {
            var products = await _productRepository.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                products = products.Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }
            ViewBag.Search = q;
            return View(products);
        }

        //public async Task<IActionResult> Add()
        //{
        //    var categories = await _categoryRepository.GetAllAsync();
        //    ViewBag.Categories = new SelectList(categories, "Id", "Name");
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> Add(Product product, List<IFormFile> imageFiles)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        if (imageFiles != null && imageFiles.Count > 0)
        //        {
        //            product.Images = new List<ProductImage>();
        //            foreach (var file in imageFiles)
        //            {
        //                if (file.Length > 0)
        //                {
        //                    var savedPath = await SaveImage(file);
        //                    // Ảnh đầu tiên làm ImageUrl chính
        //                    if (string.IsNullOrEmpty(product.ImageUrl))
        //                    {
        //                        product.ImageUrl = savedPath;
        //                    }
        //                    product.Images.Add(new ProductImage { Url = savedPath });
        //                }
        //            }
        //        }

        //        await _productRepository.AddAsync(product);
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var categories = await _categoryRepository.GetAllAsync();
        //    ViewBag.Categories = new SelectList(categories, "Id", "Name");
        //    return View(product);
        //}

        //private async Task<string> SaveImage(IFormFile image)
        //{
        //    var savePath = Path.Combine("wwwroot/images", image.FileName);
        //    using (var fileStream = new FileStream(savePath, FileMode.Create))
        //    {
        //        await image.CopyToAsync(fileStream);
        //    }
        //    return "/images/" + image.FileName;
        //}

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        //public async Task<IActionResult> Update(int id)
        //{
        //    var product = await _productRepository.GetByIdAsync(id);
        //    if (product == null) return NotFound();

        //    var categories = await _categoryRepository.GetAllAsync();
        //    ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
        //    return View(product);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Update(int id, Product product, List<IFormFile> imageFiles)
        //{
        //    ModelState.Remove("ImageUrl");

        //    if (id != product.Id) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        var existingProduct = await _productRepository.GetByIdAsync(id);
        //        if (existingProduct == null) return NotFound();

        //        // Cập nhật thông tin cơ bản
        //        existingProduct.Name = product.Name;
        //        existingProduct.Price = product.Price;
        //        existingProduct.Description = product.Description;
        //        existingProduct.CategoryId = product.CategoryId;
        //        // KHÔNG gán existingProduct.ImageUrl = product.ImageUrl ở đây
        //        // vì product.ImageUrl từ form về luôn null

        //        if (imageFiles != null && imageFiles.Count > 0)
        //        {
        //            // Có upload ảnh mới → cập nhật
        //            if (existingProduct.Images == null)
        //                existingProduct.Images = new List<ProductImage>();

        //            foreach (var file in imageFiles)
        //            {
        //                if (file.Length > 0)
        //                {
        //                    var savedPath = await SaveImage(file);

        //                    // Chỉ cập nhật ImageUrl chính nếu chưa có hoặc muốn thay ảnh đại diện
        //                    existingProduct.ImageUrl = savedPath;

        //                    existingProduct.Images.Add(new ProductImage { Url = savedPath });
        //                }
        //            }
        //        }
        //        // Không có file mới → giữ nguyên existingProduct.ImageUrl cũ

        //        await _productRepository.UpdateAsync(existingProduct);
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var categories = await _categoryRepository.GetAllAsync();
        //    ViewBag.Categories = new SelectList(categories, "Id", "Name");
        //    return View(product);
        //}

        //public async Task<IActionResult> Delete(int id)
        //{
        //    var product = await _productRepository.GetByIdAsync(id);
        //    if (product == null) return NotFound();
        //    return View(product);
        //}

        //[HttpPost, ActionName("DeleteConfirmed")]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    await _productRepository.DeleteAsync(id);
        //    return RedirectToAction(nameof(Index));
        //}
    }
}