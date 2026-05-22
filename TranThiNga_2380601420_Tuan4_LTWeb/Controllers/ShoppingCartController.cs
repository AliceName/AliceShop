using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using TranThiNga_2380601420_Tuan4_LTWeb.Extensions;
using TranThiNga_2380601420_Tuan4_LTWeb.Models;
using TranThiNga_2380601420_Tuan4_LTWeb.Repositories;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private const string CartSessionKey = "Cart";
        private const string SelectedIdsSessionKey = "CartSelectedIds";

        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository,
            IWebHostEnvironment env)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await LoadCartForCurrentUserAsync();
            var selectedIds = SyncSelectedIdsWithCart(cart, GetSelectedIdsFromSession());
            SaveSelectedIdsToSession(selectedIds);

            var model = new CartIndexViewModel
            {
                Cart = cart,
                SelectedProductIds = selectedIds
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedToCheckout(List<int> selectedProductIds)
        {
            var cart = await LoadCartForCurrentUserAsync();
            if (!cart.Items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var validIds = ValidateSelectedIds(cart, selectedProductIds);
            if (!validIds.Any())
            {
                TempData["CartError"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            SaveSelectedIdsToSession(validIds);
            return RedirectToAction(nameof(Checkout));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var fullCart = await LoadCartForCurrentUserAsync();
            if (!fullCart.Items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var selectedIds = SyncSelectedIdsWithCart(fullCart, GetSelectedIdsFromSession());
            SaveSelectedIdsToSession(selectedIds);

            var selectedItems = fullCart.Items
                .Where(i => selectedIds.Contains(i.ProductId))
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["CartError"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var model = new CheckoutViewModel
            {
                Cart = new ShoppingCart { Items = selectedItems },
                SelectedProductIds = selectedItems.Select(i => i.ProductId).ToList()
            };

            if (user != null)
            {
                model.RecipientName = user.FullName ?? string.Empty;
                model.Email = user.Email ?? string.Empty;
                model.Phone = user.PhoneNumber ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(user.Address))
                {
                    model.AddressLine = user.Address;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var fullCart = await LoadCartForCurrentUserAsync();
            if (!fullCart.Items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var selectedIds = ValidateSelectedIds(fullCart, model.SelectedProductIds);
            if (!selectedIds.Any())
            {
                TempData["CartError"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var selectedItems = fullCart.Items
                .Where(i => selectedIds.Contains(i.ProductId))
                .ToList();

            model.Cart = new ShoppingCart { Items = selectedItems };
            model.SelectedProductIds = selectedIds.ToList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var grandTotal = model.GrandTotal;

            var completedOrder = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                Status = "Completed",
                ShippingAddress = model.BuildShippingAddress(),
                Notes = model.BuildOrderNotes(),
                TotalPrice = grandTotal,
                UpdatedAt = DateTime.UtcNow,
                OrderDetails = selectedItems.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };
            _context.Orders.Add(completedOrder);

            var remainingItems = fullCart.Items
                .Where(i => !selectedIds.Contains(i.ProductId))
                .ToList();

            var remainingCart = new ShoppingCart { Items = remainingItems };
            HttpContext.Session.SetObjectAsJson(CartSessionKey, remainingCart);
            SaveSelectedIdsToSession(remainingItems.Select(i => i.ProductId).ToList());

            var cartOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.UserId == user.Id && o.Status == "Cart");

            if (cartOrder != null)
            {
                var detailsToRemove = cartOrder.OrderDetails
                    .Where(d => selectedIds.Contains(d.ProductId))
                    .ToList();

                foreach (var detail in detailsToRemove)
                {
                    cartOrder.OrderDetails.Remove(detail);
                }

                if (!cartOrder.OrderDetails.Any())
                {
                    _context.Orders.Remove(cartOrder);
                }
                else
                {
                    cartOrder.TotalPrice = remainingItems.Sum(i => i.Price * i.Quantity);
                    cartOrder.UpdatedAt = DateTime.UtcNow;
                    _context.Orders.Update(cartOrder);
                }
            }

            await _context.SaveChangesAsync();

            if (!remainingItems.Any())
            {
                HttpContext.Session.Remove(CartSessionKey);
                HttpContext.Session.Remove(SelectedIdsSessionKey);
            }

            TempData["OrderMessage"] = remainingItems.Any()
                ? $"Đặt hàng thành công! Còn {remainingItems.Count} món trong giỏ hàng."
                : "Đặt hàng thành công!";

            return View("OrderCompleted", completedOrder.Id);
        }

        [HttpGet]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await GetProductFromDatabase(productId);
            if (product == null) return NotFound();

            var candidate = EnsureLeadingSlashOrPlaceholder(product.ImageUrl);
            var productImage = WebRootFileExists(candidate) ? candidate : "/images/placeholder.png";

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CartSessionKey) ?? new ShoppingCart();
            var isNewItem = !cart.Items.Any(i => i.ProductId == productId);

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                if (quantity <= 0)
                {
                    cart.RemoveItem(productId);
                    RemoveProductFromSelection(productId);
                }
                else
                {
                    existingItem.Quantity = quantity;
                    existingItem.Price = product.Price;
                    existingItem.Name = product.Name;
                    existingItem.ImageUrl = productImage;
                    existingItem.Description = product.Description;
                }
            }
            else if (quantity > 0)
            {
                cart.AddItem(new CartItem
                {
                    ProductId = productId,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = productImage,
                    Description = product.Description
                });
            }

            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);

            if (isNewItem && quantity > 0)
            {
                AddProductToSelection(productId);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await SaveCartAsOrder(cart, user.Id);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CartSessionKey);
            if (cart is not null)
            {
                cart.RemoveItem(productId);
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }

            RemoveProductFromSelection(productId);

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var persisted = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.UserId == user.Id && o.Status == "Cart");

                if (persisted != null)
                {
                    persisted.OrderDetails.RemoveAll(d => d.ProductId == productId);
                    persisted.TotalPrice = persisted.OrderDetails.Sum(d => d.Price * d.Quantity);
                    persisted.UpdatedAt = DateTime.UtcNow;
                    if (!persisted.OrderDetails.Any())
                    {
                        _context.Orders.Remove(persisted);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private HashSet<int> GetSelectedIdsFromSession()
        {
            return HttpContext.Session.GetObjectFromJson<HashSet<int>>(SelectedIdsSessionKey)
                   ?? new HashSet<int>();
        }

        private void SaveSelectedIdsToSession(IEnumerable<int> productIds)
        {
            HttpContext.Session.SetObjectAsJson(SelectedIdsSessionKey, productIds.ToHashSet());
        }

        private static HashSet<int> SyncSelectedIdsWithCart(ShoppingCart cart, HashSet<int> selectedIds)
        {
            var cartIds = cart.Items.Select(i => i.ProductId).ToHashSet();
            selectedIds.RemoveWhere(id => !cartIds.Contains(id));

            if (!cart.Items.Any())
            {
                return new HashSet<int>();
            }

            if (!selectedIds.Any())
            {
                return cartIds;
            }

            return selectedIds;
        }

        private static List<int> ValidateSelectedIds(ShoppingCart cart, List<int>? requestedIds)
        {
            if (requestedIds == null || !requestedIds.Any())
            {
                return new List<int>();
            }

            var cartIds = cart.Items.Select(i => i.ProductId).ToHashSet();
            return requestedIds.Where(cartIds.Contains).Distinct().ToList();
        }

        private void AddProductToSelection(int productId)
        {
            var selected = GetSelectedIdsFromSession();
            selected.Add(productId);
            SaveSelectedIdsToSession(selected);
        }

        private void RemoveProductFromSelection(int productId)
        {
            var selected = GetSelectedIdsFromSession();
            selected.Remove(productId);
            SaveSelectedIdsToSession(selected);
        }

        private async Task<Product?> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }

        private async Task<ShoppingCart> LoadCartForCurrentUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var persisted = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.UserId == user.Id && o.Status == "Cart");

                if (persisted != null)
                {
                    var cart = new ShoppingCart();
                    foreach (var it in persisted.OrderDetails)
                    {
                        var p = await _productRepository.GetByIdAsync(it.ProductId);
                        var candidate = EnsureLeadingSlashOrPlaceholder(p?.ImageUrl);
                        var imageForCart = WebRootFileExists(candidate) ? candidate : "/images/placeholder.png";

                        cart.Items.Add(new CartItem
                        {
                            ProductId = it.ProductId,
                            Name = p?.Name ?? it.ProductId.ToString(),
                            Price = it.Price,
                            Quantity = it.Quantity,
                            ImageUrl = imageForCart,
                            Description = p?.Description
                        });
                    }

                    HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
                    return cart;
                }
            }

            return HttpContext.Session.GetObjectFromJson<ShoppingCart>(CartSessionKey) ?? new ShoppingCart();
        }

        private async Task SaveCartAsOrder(ShoppingCart cart, string userId)
        {
            var persisted = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

            if (persisted == null)
            {
                persisted = new Order
                {
                    UserId = userId,
                    Status = "Cart",
                    OrderDate = DateTime.UtcNow,
                    TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity),
                    UpdatedAt = DateTime.UtcNow,
                    OrderDetails = cart.Items.Select(i => new OrderDetail
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                };
                _context.Orders.Add(persisted);
            }
            else
            {
                var toRemove = persisted.OrderDetails
                    .Where(d => !cart.Items.Any(ci => ci.ProductId == d.ProductId))
                    .ToList();
                foreach (var r in toRemove) persisted.OrderDetails.Remove(r);

                foreach (var ci in cart.Items)
                {
                    var detail = persisted.OrderDetails.FirstOrDefault(d => d.ProductId == ci.ProductId);
                    if (detail == null)
                    {
                        persisted.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = ci.ProductId,
                            Quantity = ci.Quantity,
                            Price = ci.Price
                        });
                    }
                    else
                    {
                        detail.Quantity = ci.Quantity;
                        detail.Price = ci.Price;
                    }
                }

                persisted.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
                persisted.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Update(persisted);
            }

            await _context.SaveChangesAsync();
        }

        private string EnsureLeadingSlashOrPlaceholder(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "/images/placeholder.png";
            return url.StartsWith("/") ? url : "/" + url;
        }

        private bool WebRootFileExists(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return false;
            var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physical = Path.Combine(_env.WebRootPath ?? "wwwroot", relativePath);
            return System.IO.File.Exists(physical);
        }
    }
}
