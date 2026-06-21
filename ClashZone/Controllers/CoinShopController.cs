using ClashZone.App.Domains;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashZone.Controllers
{
    public class CoinShopController : Controller
    {
        private readonly ICoinShopService _shop;
        private readonly ICoinWalletService _wallet;

        public CoinShopController(ICoinShopService shop, ICoinWalletService wallet)
        {
            _shop = shop;
            _wallet = wallet;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var catalog = await _shop.GetCatalogAsync();
            ViewBag.Balance = User.Identity?.IsAuthenticated == true
                ? await _wallet.GetBalanceAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                : 0;

            return View(catalog);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var p = await _shop.GetProductAsync(id);
            if (p == null) return NotFound();
            ViewBag.Balance = User.Identity?.IsAuthenticated == true
                ? await _wallet.GetBalanceAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
                : 0;
            return View(p);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout(int id)
        {
            var product = await _shop.GetProductAsync(id);
            if (product == null || !product.IsActive)
            {
                TempData["ShopError"] = "Produkt niedostępny.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var model = new CheckoutViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ClashCoins = product.ClashCoins,
                ImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                    ?? product.Images.FirstOrDefault()?.Url,
                Balance = await _wallet.GetBalanceAsync(userId)
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var product = await _shop.GetProductAsync(model.ProductId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            model.ProductName = product?.Name ?? model.ProductName;
            model.ClashCoins = product?.ClashCoins ?? model.ClashCoins;
            model.ImageUrl = product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                ?? product?.Images.FirstOrDefault()?.Url;
            model.Balance = await _wallet.GetBalanceAsync(userId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var shipping = new ShippingAddressDomain
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Street = model.Street,
                PostalCode = model.PostalCode,
                City = model.City,
                Country = model.Country
            };

            var (ok, error) = await _shop.PurchaseAsync(model.ProductId, userId, shipping);
            if (!ok)
            {
                TempData["ShopError"] = error;
                return RedirectToAction(nameof(Details), new { id = model.ProductId });
            }

            TempData["ShopOk"] = "Zakup zrealizowany! Sprawdź zakładkę zamówień w profilu.";
            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }
    }
}

