using ClashZone.App.Ports.DataAccess;
using ClashZone.App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashZone.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionController(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<IActionResult> Index()
        {
            var plans = await _subscriptionRepository.GetAllPlansAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            var model = new SubscriptionIndexViewModel
            {
                Plans = plans.ToList(),
                ActiveSubscription = activeSubscription
            };
            if (TempData.ContainsKey("SubscriptionError"))
            {
                ViewBag.SubscriptionError = TempData["SubscriptionError"];
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(int planId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _subscriptionRepository.CreateSubscriptionAsync(userId, planId);
            return RedirectToAction(nameof(Index));
        }
    }
}
