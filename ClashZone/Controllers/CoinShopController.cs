using ClashZone.Services.Interfaces;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (ok, error) = await _shop.PurchaseAsync(id, userId);
            if (!ok)
            {
                TempData["ShopError"] = error;
            }
            else
            {
                TempData["ShopOk"] = "Zakup zrealizowany! Sprawdź zakładkę zamówień w profilu.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

