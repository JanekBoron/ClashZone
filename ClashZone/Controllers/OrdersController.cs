using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Panel administratora do zarządzania zamówieniami (realizacjami nagród
    /// ze sklepu). Pozwala przeglądać wszystkie zamówienia i zmieniać ich status.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("admin/orders")]
    public class OrdersController : Controller
    {
        private readonly IProductRedeemRepository _orders;
        private readonly IUserRepository _users;

        public OrdersController(IProductRedeemRepository orders, IUserRepository users)
        {
            _orders = orders;
            _users = users;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var orders = await _orders.GetAllAsync();

            // Rozwiązujemy loginy zamawiających (z buforowaniem, by uniknąć powtórek).
            var nameCache = new Dictionary<string, string>();
            var items = new List<OrderListItemViewModel>(orders.Count);
            foreach (var o in orders)
            {
                if (!nameCache.TryGetValue(o.UserId, out var userName))
                {
                    var user = await _users.FindByIdAsync(o.UserId);
                    userName = user?.UserName ?? o.UserId;
                    nameCache[o.UserId] = userName;
                }
                items.Add(new OrderListItemViewModel
                {
                    Id = o.Id,
                    UserName = userName,
                    ProductName = o.Product?.Name ?? "—",
                    ClashCoins = o.Product?.ClashCoins ?? 0,
                    RedeemedAt = o.RedeemedAt,
                    Status = o.Status,
                    Notes = o.Notes
                });
            }
            return View(items);
        }

        [HttpPost("status")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? notes)
        {
            if (!OrderStatus.IsValid(status))
            {
                TempData["OrderError"] = "Nieprawidłowy status zamówienia.";
                return RedirectToAction(nameof(Index));
            }
            await _orders.UpdateStatusAsync(id, status, notes);
            TempData["OrderOk"] = "Status zamówienia został zaktualizowany.";
            return RedirectToAction(nameof(Index));
        }
    }
}
