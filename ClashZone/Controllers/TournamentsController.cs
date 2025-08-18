using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using DataAccess;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Kontroler obsługujący wszystkie akcje związane z turniejami, w tym
    /// wyświetlanie listy nadchodzących turniejów, tworzenie nowych 
    /// turniejów,
    /// dołączanie do turniejów oraz prezentację drabinki.  Dodano obsługę
    /// subskrypcji, dzięki czemu turnieje oznaczone jako premium wymagają
    /// aktywnego planu Premium lub Ultra.  Zintegrowano również wysyłanie
    /// wiadomości e‑mail po pomyślnym dołączeniu do turnieju.
    /// </summary>
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

        public TournamentsController(
            UserManager<ClashUser> userManager,
            ISubscriptionRepository subscriptionRepository,
            IChatService chatService,
            IBracketService bracketService,
            ITournamentService tournamentService,
            ITournamentsRepository tournamentsRepository,
            IEmailService emailService,
            IMatchesService matchesService)
        {
            _userManager = userManager;
            _subscriptionRepository = subscriptionRepository;
            _chatService = chatService;
            _bracketService = bracketService;
            _tournamentService = tournamentService;
            _tournamentsRepository = tournamentsRepository;
            _emailService = emailService;
            _matchesService = matchesService;
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

        /// <summary>
        /// Displays detailed statistics for a single match within a
        /// tournament.  Delegates to the matches service to assemble
        /// per‑player statistics, team names and captain avatars.
        /// </summary>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <param name="id">Identifier of the match.</param>
        /// <param name="matchesService">Service used to build the match details view model.</param>
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

        /// <summary>
        /// Displays a page containing rules for a specific tournament.
        /// The rules shown are determined based on the tournament's game
        /// title, with fallback default rules for unknown games.
        /// </summary>
        /// <param name="id">The identifier of the tournament.</param>
        [HttpGet]
        public async Task<IActionResult> Rules(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            var rules = GetRulesForGame(tournament.GameTitle);
            var model = new RulesViewModel
            {
                Tournament = tournament,
                Rules = rules
            };

            return View(model);
        }

        /// <summary>
        /// Builds a list of default rules for a game.  This is a simple
        /// demonstration stub that returns a set of example rules based
        /// on the provided game title.  For unrecognised games a set of
        /// generic fair‑play rules is returned.
        /// </summary>
        /// <param name="gameTitle">The title of the game.</param>
        private List<string> GetRulesForGame(string? gameTitle)
        {
            var rules = new List<string>();
            if (string.IsNullOrWhiteSpace(gameTitle))
            {
                rules.Add("Obowiązują standardowe zasady fair play.");
                rules.Add("Każdy mecz musi zostać rozegrany w ustalonym terminie.");
                return rules;
            }

            var lower = gameTitle.Trim().ToLowerInvariant();
            switch (lower)
            {
                case "counter strike 2":
                case "counter-strike 2":
                case "cs2":
                    rules.Add("Każdy mecz rozgrywany jest w formacie best of 3 (do dwóch wygranych map).\n");
                    rules.Add("Zabrania się wykorzystywania błędów gry oraz niedozwolonych skryptów.");
                    rules.Add("Skład drużyny musi liczyć dokładnie 5 zawodników.");
                    break;
                case "league of legends":
                case "lol":
                    rules.Add("Mecze rozgrywane są na mapie Summoner's Rift w trybie Draft Pick.");
                    rules.Add("Zakaz używania bohaterów spoza oficjalnej rotacji turniejowej.");
                    rules.Add("Składy drużyn są zamrożone po zakończeniu zapisów.");
                    break;
                default:
                    rules.Add("Obowiązują standardowe zasady fair play.");
                    rules.Add("Każdy mecz musi zostać rozegrany w ustalonym terminie.");
                    break;
            }
            return rules;
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

            // Retrieve teams and build view models with friendly names and member lists
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(id);
            var teamViewModels = new List<ParticipantTeamViewModel>();
            foreach (var team in teams)
            {
                // Determine a friendly team name
                string? name = team.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }

                // Gather member display names
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

        /// <summary>
        /// Posts a message to the dedicated problem report chat.  The teamId
        /// should be provided by the form.  Non‑administrators will ignore
        /// the provided teamId and automatically route the report to their
        /// own team.  Administrators may specify a team to respond to.
        /// </summary>
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
                // Either the tournament was not found or the bracket isn't ready yet
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(model);
        }

        /// <summary>
        /// Generates random results for all matches in the bracket and displays the updated bracket.
        /// This action is invoked when the user presses the 'Wyniki' button on the bracket view.
        /// </summary>
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
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTournament(
            Tournament tournament,
            // Prize parameters for 1st place
            string prizeOption1,
            string? prizeDescription1,
            int? prizeCoins1,
            // Prize parameters for 2nd place
            string prizeOption2,
            string? prizeDescription2,
            int? prizeCoins2,
            // Prize parameters for 3rd place
            string prizeOption3,
            string? prizeDescription3,
            int? prizeCoins3)
        {
            if (ModelState.IsValid)
            {
                // Always set game title to Counter Strike 2
                tournament.GameTitle = "Counter Strike 2";

                // Helper local function to compute a single prize string based on option
                string ComputePrize(string option, string? desc, int? coins)
                {
                    if (option == "text")
                    {
                        return desc ?? string.Empty;
                    }
                    else if (option == "coins" && coins.HasValue)
                    {
                        return $"{coins.Value} ClashCoins";
                    }
                    return string.Empty;
                }

                // Build prizes for each place
                string first = ComputePrize(prizeOption1, prizeDescription1, prizeCoins1);
                string second = ComputePrize(prizeOption2, prizeDescription2, prizeCoins2);
                string third = ComputePrize(prizeOption3, prizeDescription3, prizeCoins3);

                // Combine into a single string separated by pipe symbol.  This format can be
                // parsed later when displaying or editing the tournament.
                tournament.Prize = string.Join("|", new[] { first, second, third });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _tournamentService.CreateTournamentAsync(tournament, userId);
                return RedirectToAction(nameof(Index));
            }
            return View(tournament);
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
            // If a new team was created and the tournament is not a solo format, generate an invitation link
            if (result.Team != null && result.TournamentFormat != null && !result.TournamentFormat.Equals("1v1", StringComparison.OrdinalIgnoreCase))
            {
                var inviteLink = Url.Action(nameof(JoinTeam), nameof(TournamentsController).Replace("Controller", string.Empty), new { teamId = result.Team.Id, code = result.Team.JoinCode }, Request.Scheme);
                TempData["InviteLink"] = inviteLink;
            }
            // Send tournament registration confirmation email
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && tournament != null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendTournamentRegistrationConfirmationAsync(user.Email, tournament.Name);
            }
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

        // ---------------- Admin/Organizer management actions ----------------

        /// <summary>
        /// Displays a list of upcoming tournaments for organizers and admins with options to edit or delete.
        /// </summary>
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> Manage()
        {
            var tournaments = await _tournamentService.GetUpcomingTournamentsAsync(null);
            return View("Manage", tournaments);
        }

        /// <summary>
        /// Shows the edit form for a specific tournament.
        /// </summary>
        /// <param name="id">Identifier of the tournament to edit.</param>
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            return View(tournament);
        }

        /// <summary>
        /// Processes the submission of the edit tournament form.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Tournament tournament,
            // Prize parameters for 1st place
            string prizeOption1,
            string? prizeDescription1,
            int? prizeCoins1,
            // Prize parameters for 2nd place
            string prizeOption2,
            string? prizeDescription2,
            int? prizeCoins2,
            // Prize parameters for 3rd place
            string prizeOption3,
            string? prizeDescription3,
            int? prizeCoins3)
        {
            if (id != tournament.Id)
            {
                return BadRequest();
            }
            if (ModelState.IsValid)
            {
                // Always set game title to Counter Strike 2
                tournament.GameTitle = "Counter Strike 2";

                string ComputePrize(string option, string? desc, int? coins)
                {
                    if (option == "text")
                    {
                        return desc ?? string.Empty;
                    }
                    else if (option == "coins" && coins.HasValue)
                    {
                        return $"{coins.Value} ClashCoins";
                    }
                    return string.Empty;
                }
                string first = ComputePrize(prizeOption1, prizeDescription1, prizeCoins1);
                string second = ComputePrize(prizeOption2, prizeDescription2, prizeCoins2);
                string third = ComputePrize(prizeOption3, prizeDescription3, prizeCoins3);
                tournament.Prize = string.Join("|", new[] { first, second, third });

                await _tournamentsRepository.UpdateTournamentAsync(tournament);
                return RedirectToAction(nameof(Manage));
            }
            return View(tournament);
        }

        /// <summary>
        /// Deletes the specified tournament and redirects back to the management view.
        ///
        /// Tylko administratorzy mogą usuwać turnieje.  Organizatorzy nie mają
        /// takiej opcji, dzięki czemu ryzyko przypadkowego usunięcia danych
        /// zostaje zminimalizowane.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _tournamentsRepository.DeleteTournamentAsync(id);
            return RedirectToAction(nameof(Manage));
        }
    }
}