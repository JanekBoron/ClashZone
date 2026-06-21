using ClashZone.App.Domains;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClashZone.Controllers
{
    public class LeagueController : Controller
    {
        private readonly ILeagueService _leagueService;

        public LeagueController(ILeagueService leagueService)
        {
            _leagueService = leagueService;
        }

        private string? UserId => User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;


        public async Task<IActionResult> Index()
        {
            var seasons = await _leagueService.GetSeasonsAsync();
            return View(seasons);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var model = await _leagueService.GetSeasonDetailsAsync(id, UserId);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int seasonId, int leagueTeamId)
        {
            var result = await _leagueService.RegisterTeamAsync(seasonId, leagueTeamId, UserId!);
            if (result.SeasonNotFound) return NotFound();
            if (result.RequiresLeaguePackage)
            {
                TempData["LeagueError"] = "Rejestracja do ligi wymaga aktywnego pakietu League lub Ultra.";
                return RedirectToAction("Index", "Subscription");
            }
            if (result.RegistrationClosed)
                TempData["LeagueError"] = "Rejestracja do tego sezonu jest zamknięta.";
            else if (result.NotCaptain)
                TempData["LeagueError"] = "Tylko kapitan może zgłosić drużynę.";
            else if (result.RosterTooSmall)
                TempData["LeagueError"] = $"Drużyna musi liczyć co najmniej {result.RequiredRoster} graczy.";
            else if (result.AlreadyRegistered)
                TempData["LeagueInfo"] = "Ta drużyna jest już zgłoszona do sezonu.";
            else if (result.Success)
                TempData["LeagueInfo"] = "Drużyna została zgłoszona do sezonu!";

            return RedirectToAction(nameof(Details), new { id = seasonId });
        }

        [Authorize]
        public async Task<IActionResult> Standings(int id)
        {
            var season = await _leagueService.GetSeasonDetailsAsync(id, UserId);
            if (season == null) return NotFound();
            return View(season);
        }

        [Authorize]
        public async Task<IActionResult> Matches(int id)
        {
            var model = await _leagueService.GetMatchesAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> MatchDetails(int seasonId, int id)
        {
            var model = await _leagueService.GetMatchDetailsAsync(seasonId, id);
            if (model == null) return NotFound();
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Bracket(int id)
        {
            var model = await _leagueService.GetPlayoffBracketAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Participants(int id)
        {
            var model = await _leagueService.GetParticipantsAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        public async Task<IActionResult> Rules(int id)
        {
            var model = await _leagueService.GetRulesAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Chat(int id)
        {
            var isAdmin = User.IsInRole("Admin");
            var model = await _leagueService.GetChatAsync(id, UserId, isAdmin);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int id, string message, bool toTeam)
        {
            await _leagueService.PostMessageAsync(id, UserId!, message, toTeam);
            return RedirectToAction(nameof(Chat), new { id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostReportMessage(int id, string message, int? teamId)
        {
            var isAdmin = User.IsInRole("Admin");
            await _leagueService.PostReportMessageAsync(id, UserId!, message, teamId, isAdmin);
            return RedirectToAction(nameof(Chat), new { id });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Schedule(int matchId)
        {
            var model = await _leagueService.GetScheduleAsync(matchId, UserId!);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(int matchId, DateTime proposedTime, string? server)
        {
            await _leagueService.ProposeScheduleAsync(matchId, UserId!, proposedTime, server);
            var model = await _leagueService.GetScheduleAsync(matchId, UserId!);
            if (model == null) return NotFound();
            TempData["LeagueInfo"] = "Termin meczu został zaktualizowany.";
            return RedirectToAction(nameof(Matches), new { id = model.Season.Id });
        }

        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> Manage()
        {
            var seasons = await _leagueService.GetManageListAsync();
            return View(seasons);
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpGet]
        public IActionResult Create() => View(new SeasonFormViewModel());

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SeasonFormViewModel model)
        {
            ValidateGame(model);
            if (!ModelState.IsValid) return View(model);
            await _leagueService.CreateSeasonAsync(model, UserId!);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _leagueService.GetSeasonFormAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SeasonFormViewModel model)
        {
            if (id != model.Id) return BadRequest();
            ValidateGame(model);
            if (!ModelState.IsValid) return View(model);
            await _leagueService.UpdateSeasonAsync(model);
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseRegistration(int id)
        {
            await _leagueService.CloseRegistrationAndPruneAsync(id);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMatchups(int id)
        {
            await _leagueService.GenerateOpeningMatchupsAsync(id);
            return RedirectToAction(nameof(Matches), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateWeek(int id)
        {
            await _leagueService.GenerateNextWeekAsync(id);
            return RedirectToAction(nameof(Matches), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateResults(int id)
        {
            await _leagueService.GenerateResultsAsync(id);
            return RedirectToAction(nameof(Matches), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePlayoffs(int id)
        {
            await _leagueService.GeneratePlayoffsAsync(id);
            return RedirectToAction(nameof(Bracket), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulatePlayoffMatch(int id)
        {
            await _leagueService.SimulateNextPlayoffMatchAsync(id);
            return RedirectToAction(nameof(Bracket), new { id });
        }

        private void ValidateGame(SeasonFormViewModel model)
        {
            var game = GameCatalog.Normalize(model.Game);
            if (game == null)
            {
                ModelState.AddModelError(nameof(model.Game), "Nieobsługiwany tytuł gry.");
                return;
            }
            if (!GameCatalog.IsFormatAllowed(game, model.Format))
            {
                ModelState.AddModelError(nameof(model.Format),
                    $"Format {model.Format} jest niedozwolony dla gry {game}.");
            }
        }
    }
}
