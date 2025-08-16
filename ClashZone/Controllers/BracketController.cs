using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Controller responsible for bracket‑specific actions such as simulating a single match.
    /// A dedicated controller is used to avoid modifying the existing TournamentsController.  It
    /// leverages the IBracketService to simulate matches and then reuses the existing bracket
    /// view for display.  The routes are invoked from the bracket view via forms with hidden
    /// fields indicating the tournament, round and match indices.
    /// </summary>
    [Authorize]
    public class BracketController : Controller
    {
        private readonly IBracketService _bracketService;

        public BracketController(IBracketService bracketService)
        {
            _bracketService = bracketService;
        }

        /// <summary>
        /// Simulates a single match within a tournament bracket.  When invoked, this action
        /// generates a random score for the specified match, persists the result along with
        /// per‑player statistics and returns the updated bracket view.  If the simulation
        /// cannot be performed (for example due to undefined participants), the user is
        /// redirected back to the tournament bracket without changes.
        /// </summary>
        /// <param name="id">Identifier of the tournament.</param>
        /// <param name="roundNumber">One‑based index of the round containing the match.</param>
        /// <param name="matchNumber">One‑based index of the match within the round.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulateMatch(int id, int roundNumber, int matchNumber)
        {
            var model = await _bracketService.SimulateMatchAsync(id, roundNumber, matchNumber);
            if (model == null)
            {
                // Simulation could not be performed; redirect to the original bracket
                return RedirectToAction("Bracket", "Tournaments", new { id });
            }
            // Reuse the existing bracket view from the Tournaments folder
            return View("~/Views/Tournaments/Bracket.cshtml", model);
        }
    }
}