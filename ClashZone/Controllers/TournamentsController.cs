using AutoMapper;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClashZone.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly UserManager<ClashUser> _userManager;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IChatService _chatService;
        private readonly IBracketService _bracketService;
        private readonly ITournamentService _tournamentService;
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly IEmailService _emailService;
        private readonly IMatchesService _matchesService;
        private readonly IMapper _mapper;

        public TournamentsController(
            UserManager<ClashUser> userManager,
            ISubscriptionRepository subscriptionRepository,
            IChatService chatService,
            IBracketService bracketService,
            ITournamentService tournamentService,
            ITournamentsRepository tournamentsRepository,
            IEmailService emailService,
            IMatchesService matchesService,
            IMapper mapper)
        {
            _userManager = userManager;
            _subscriptionRepository = subscriptionRepository;
            _chatService = chatService;
            _bracketService = bracketService;
            _tournamentService = tournamentService;
            _tournamentsRepository = tournamentsRepository;
            _emailService = emailService;
            _matchesService = matchesService;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Matches(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            var matches = await _matchesService.GetMatchesForTournamentAsync(id);
            var viewModel = new TournamentMatchesViewModel
            {
                Tournament = tournament,
                Matches = matches
            };
            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MatchDetails(int tournamentId, int id)
        {
            var model = await _matchesService.GetMatchDetailsAsync(tournamentId, id);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Rules(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            // Preferuj reguły wpisane przez organizatora; w razie ich braku użyj
            // domyślnych reguł właściwych dla danej gry.
            var rules = !string.IsNullOrWhiteSpace(tournament.Rules)
                ? tournament.Rules
                    .Split('\n')
                    .Select(r => r.Trim())
                    .Where(r => r.Length > 0)
                    .ToList()
                : GameCatalog.DefaultRules(tournament.GameTitle);

            var model = new RulesViewModel
            {
                Tournament = tournament,
                Rules = rules
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ParticipantsList(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(id);
            var teamViewModels = new List<ParticipantTeamViewModel>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }

                var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
                var memberNames = new List<string>();
                foreach (var userId in memberIds)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        memberNames.Add(user.UserName ?? user.Id);
                    }
                }

                teamViewModels.Add(new ParticipantTeamViewModel
                {
                    TeamId = team.Id,
                    Name = name ?? string.Empty,
                    Members = memberNames
                });
            }

            var model = new ParticipantsViewModel
            {
                Tournament = tournament,
                Teams = teamViewModels
            };

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Chat(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var model = await _chatService.GetChatAsync(id, userId, isAdmin);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int id, string message, bool toTeam)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _chatService.PostMessageAsync(id, userId, message, toTeam);
            return RedirectToAction(nameof(Chat), new { id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostReportMessage(int id, string message, int? teamId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            await _chatService.PostReportMessageAsync(id, userId, message, teamId, isAdmin);
            return RedirectToAction(nameof(Chat), new { id });
        }

        [Authorize]
        public async Task<IActionResult> Bracket(int id)
        {
            var model = await _bracketService.GetBracketAsync(id);
            if (model == null)
            {
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateResults(int id)
        {
            var model = await _bracketService.GetBracketWithResultsAsync(id);
            if (model == null)
            {
                return RedirectToAction(nameof(Details), new { id });
            }
            return View("Bracket", model);
        }

        public async Task<IActionResult> Index(string? format)
        {
            var tournaments = await _tournamentService.GetUpcomingTournamentsAsync(format);
            ViewBag.SelectedFormat = format;
            ViewBag.IsMy = false;
            return View(tournaments);
        }

        [Authorize]
        public async Task<IActionResult> MyTournaments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tournaments = await _tournamentService.GetUserTournamentsAsync(userId);
            ViewBag.SelectedFormat = null;
            ViewBag.IsMy = true;
            return View("Index", tournaments);
        }

        [Authorize(Roles = "Organizer,Admin")]
        public IActionResult Create()
        {
            return View(new TournamentFormViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTournament(
            TournamentFormViewModel model,
            int? prizeCoins1, int? prizeCoins2, int? prizeCoins3)
        {
            ValidateGameAndFormat(model);
            if (ModelState.IsValid)
            {
                var tournament = _mapper.Map<TournamentDomain>(model);
                tournament.GameTitle = GameCatalog.Normalize(model.GameTitle)!;
                tournament.FirstPlacePrize = ComputePrize(prizeCoins1);
                tournament.SecondPlacePrize = ComputePrize(prizeCoins2);
                tournament.ThirdPlacePrize = ComputePrize(prizeCoins3);

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _tournamentService.CreateTournamentAsync(tournament, userId);
                return RedirectToAction(nameof(Index));
            }
            // Akcja nazywa się CreateTournament, więc jawnie wskazujemy widok Create.
            return View("Create", model);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            var viewModel = await _tournamentService.GetTournamentDetailsAsync(id, userId);
            if (viewModel == null)
            {
                return NotFound();
            }
            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _tournamentService.JoinTournamentAsync(id, userId);
            if (result.NotFound)
            {
                return NotFound();
            }
            if (result.RequiresSubscription)
            {
                TempData["SubscriptionError"] = "Ten turniej jest dostępny tylko dla użytkowników z pakietem Premium lub Ultra.";
                return RedirectToAction("Index", "Subscription");
            }
            if (result.AlreadyJoined)
            {
                return RedirectToAction(nameof(Details), new { id });
            }
            if (result.MaxParticipantsExceeded)
            {
                TempData["JoinError"] = "Limit uczestników został osiągnięty. Nie można dołączyć do tego turnieju.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (result.Team != null && result.TournamentFormat != null && !result.TournamentFormat.Equals("1v1", StringComparison.OrdinalIgnoreCase))
            {
                var inviteLink = Url.Action(nameof(JoinTeam), nameof(TournamentsController).Replace("Controller", string.Empty), new { teamId = result.Team.Id, code = result.Team.JoinCode }, Request.Scheme);
                TempData["InviteLink"] = inviteLink;
            }
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && tournament != null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendTournamentRegistrationConfirmationAsync(user.Email, tournament.Name);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReady(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _tournamentService.ToggleReadyAsync(id, userId);
            TempData[result.IsError ? "TeamError" : "TeamInfo"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmTeam(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _tournamentService.ConfirmTeamAsync(id, userId);
            TempData[result.IsError ? "TeamError" : "TeamInfo"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize]
        public async Task<IActionResult> JoinTeam(int teamId, string code)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tournamentId = await _tournamentService.JoinTeamAsync(teamId, userId, code);
            if (!tournamentId.HasValue)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Details), new { id = tournamentId.Value });
        }

        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> Manage()
        {
            var tournaments = await _tournamentService.GetUpcomingTournamentsAsync(null);
            return View("Manage", tournaments);
        }

        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            var model = _mapper.Map<TournamentFormViewModel>(tournament);
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            TournamentFormViewModel model,
            int? prizeCoins1, int? prizeCoins2, int? prizeCoins3)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }
            ValidateGameAndFormat(model);
            if (ModelState.IsValid)
            {
                var tournament = _mapper.Map<TournamentDomain>(model);
                tournament.GameTitle = GameCatalog.Normalize(model.GameTitle)!;
                tournament.FirstPlacePrize = ComputePrize(prizeCoins1);
                tournament.SecondPlacePrize = ComputePrize(prizeCoins2);
                tournament.ThirdPlacePrize = ComputePrize(prizeCoins3);

                await _tournamentsRepository.UpdateTournamentAsync(tournament);
                return RedirectToAction(nameof(Manage));
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _tournamentsRepository.DeleteTournamentAsync(id);
            return RedirectToAction(nameof(Manage));
        }

        private static string ComputePrize(int? coins)
        {
            // Nagrody są wyłącznie w ClashCoins. Brak/0 oznacza brak nagrody.
            return coins.HasValue && coins.Value > 0 ? $"{coins.Value} ClashCoins" : string.Empty;
        }

        /// <summary>
        /// Waliduje wybrany tytuł gry oraz format drużyn (różne gry dopuszczają
        /// różne formaty — np. League of Legends nie ma 2v2). Błędy trafiają do
        /// ModelState, aby formularz wyświetlił komunikaty.
        /// </summary>
        private void ValidateGameAndFormat(TournamentFormViewModel model)
        {
            var game = GameCatalog.Normalize(model.GameTitle);
            if (game == null)
            {
                ModelState.AddModelError(nameof(model.GameTitle), "Nieobsługiwany tytuł gry.");
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
