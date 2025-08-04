using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.DataAccess.Models;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Kontroler odpowiedzialny za prezentowanie dostępnych planów subskrypcyjnych
    /// oraz obsługę procesu zakupu.  Wymaga aby użytkownik był zalogowany.
    /// </summary>
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly UserManager<ClashUser> _userManager;

        public SubscriptionController(ISubscriptionRepository subscriptionRepository, UserManager<ClashUser> userManager)
        {
            _subscriptionRepository = subscriptionRepository;
            _userManager = userManager;
        }

        /// <summary>
        /// Wyświetla listę dostępnych planów subskrypcyjnych wraz z informacją
        /// o aktualnie aktywnej subskrypcji użytkownika (jeśli istnieje).
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var plans = await _subscriptionRepository.GetAllPlansAsync();
            var userId = _userManager.GetUserId(User);
            var activeSubscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            var model = new ViewModels.SubscriptionIndexViewModel
            {
                Plans = plans.ToList(),
                ActiveSubscription = activeSubscription
            };
            // Jeśli z TempData został przekazany komunikat o błędzie subskrypcji, przekaż go do ViewBag
            if (TempData.ContainsKey("SubscriptionError"))
            {
                ViewBag.SubscriptionError = TempData["SubscriptionError"];
            }
            return View(model);
        }

        /// <summary>
        /// Obsługuje żądanie zakupu subskrypcji.  Po utworzeniu subskrypcji użytkownik jest przekierowany do listy planów.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(int planId)
        {
            var userId = _userManager.GetUserId(User);
            await _subscriptionRepository.CreateSubscriptionAsync(userId, planId);
            return RedirectToAction(nameof(Index));
        }
    }
}