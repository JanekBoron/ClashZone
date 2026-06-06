using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Team Management — trwałe drużyny ligowe zarządzane przez użytkowników.
    /// Obejmuje tworzenie drużyn, podgląd rosteru oraz dołączanie linkiem/kodem.
    /// </summary>
    [Authorize]
    public class TeamController : Controller
    {
        private readonly ILeagueTeamService _leagueTeamService;

        public TeamController(ILeagueTeamService leagueTeamService)
        {
            _leagueTeamService = leagueTeamService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = await _leagueTeamService.GetUserTeamsAsync(userId!);
            return View(model);
        }

        [HttpGet]
        public IActionResult Create() => View(new LeagueTeamFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeagueTeamFormViewModel model)
        {
            if (!App.Domains.GameCatalog.IsValid(model.Game))
            {
                ModelState.AddModelError(nameof(model.Game), "Wybierz obsługiwaną grę.");
            }
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Podaj nazwę drużyny.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var team = await _leagueTeamService.CreateTeamAsync(model.Name, model.Game, userId!);
            return RedirectToAction(nameof(Details), new { id = team.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = await _leagueTeamService.GetTeamDetailsAsync(id, userId!);
            if (model == null)
            {
                return NotFound();
            }
            model.InviteLink = Url.Action(nameof(Join), "Team",
                new { teamId = model.Team.Id, code = model.Team.JoinCode }, Request.Scheme);
            return View(model);
        }

        public async Task<IActionResult> Join(int teamId, string code)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _leagueTeamService.JoinByCodeAsync(teamId, code, userId!);
            if (result.NotFound)
            {
                return NotFound();
            }
            if (result.InvalidCode)
            {
                TempData["TeamError"] = "Nieprawidłowy kod zaproszenia.";
            }
            else if (result.AlreadyMember)
            {
                TempData["TeamInfo"] = "Jesteś już członkiem tej drużyny.";
            }
            else if (result.UserCooldown)
            {
                TempData["TeamError"] = "Możesz dołączyć tylko do jednej drużyny na 7 dni.";
            }
            else if (result.RosterCooldown)
            {
                TempData["TeamError"] = "Do tej drużyny dołączyło już 3 graczy w ciągu ostatnich 7 dni.";
            }
            else if (result.Success)
            {
                TempData["TeamInfo"] = "Dołączono do drużyny!";
            }
            return RedirectToAction(nameof(Details), new { id = teamId });
        }
    }
}
