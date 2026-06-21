using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
namespace ClashZone.Controllers
{

    [Authorize]
    public class BracketController : Controller
    {
        private readonly IBracketService _bracketService;

        public BracketController(IBracketService bracketService)
        {
            _bracketService = bracketService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulateMatch(int id, int roundNumber, int matchNumber)
        {
            var model = await _bracketService.SimulateMatchAsync(id, roundNumber, matchNumber, User?.IsInRole("Admin") ?? false);
            if (model == null)
            {
                return RedirectToAction("Bracket", "Tournaments", new { id });
            }
            return View("~/Views/Tournaments/Bracket.cshtml", model);
        }
    }
}