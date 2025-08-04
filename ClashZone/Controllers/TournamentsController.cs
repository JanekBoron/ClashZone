using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
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
    /// wyświetlanie listy nadchodzących turniejów, tworzenie nowych turniejów,
    /// dołączanie do turniejów oraz prezentację drabinki.  Dodano obsługę
    /// subskrypcji, dzięki czemu turnieje oznaczone jako premium wymagają
    /// aktywnego planu Premium lub Ultra.
    /// </summary>
    public class TournamentsController : Controller
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly UserManager<ClashUser> _userManager;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public TournamentsController(ITournamentsRepository tournamentsRepository,
                                     UserManager<ClashUser> userManager,
                                     ISubscriptionRepository subscriptionRepository)
        {
            _tournamentsRepository = tournamentsRepository;
            _userManager = userManager;
            _subscriptionRepository = subscriptionRepository;
        }

        /// <summary>
        /// Displays the chat page for a specific tournament.  Shows both the
        /// public chat and the current user's team chat (if applicable).
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Chat(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userTeam = await _tournamentsRepository.GetUserTeamAsync(id, userId);
            var model = new ChatViewModel
            {
                Tournament = tournament,
                HasTeam = userTeam != null
            };
            model.AllMessages = await _tournamentsRepository.GetAllChatMessagesAsync(id);
            if (userTeam != null)
            {
                model.TeamMessages = await _tournamentsRepository.GetTeamChatMessagesAsync(id, userTeam.Id);
            }
            // Build dictionary of userId -> display name for messages
            var userIds = model.AllMessages.Select(m => m.UserId)
                .Concat(model.TeamMessages.Select(m => m.UserId))
                .Distinct().ToList();
            foreach (var uid in userIds)
            {
                var user = await _userManager.FindByIdAsync(uid);
                if (user != null && !model.UserNames.ContainsKey(uid))
                {
                    model.UserNames[uid] = user.UserName;
                }
            }
            return View(model);
        }

        /// <summary>
        /// Handles posting of chat messages.  Users can post to the entire
        /// tournament or to their team only.  After posting, the user is
        /// redirected back to the chat page.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int id, string message, bool toTeam)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return RedirectToAction(nameof(Chat), new { id });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? teamId = null;
            if (toTeam)
            {
                var userTeam = await _tournamentsRepository.GetUserTeamAsync(id, userId);
                if (userTeam == null)
                {
                    return NotFound();
                }
                teamId = userTeam.Id;
            }
            var chatMsg = new ChatMessage
            {
                TournamentId = id,
                TeamId = teamId,
                UserId = userId,
                Message = message,
                SentAt = DateTime.UtcNow
            };
            await _tournamentsRepository.AddChatMessageAsync(chatMsg);
            return RedirectToAction(nameof(Chat), new { id });
        }

        /// <summary>
        /// Displays the tournament bracket for a specific event.  The bracket
        /// becomes available only after the check‑in period has closed (i.e. the
        /// start time has been reached).  If the tournament is not yet ready,
        /// users are redirected back to the details page.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Bracket(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null) return NotFound();
            // Check that check‑in is closed: bracket available after start time
            if (DateTime.UtcNow < tournament.StartDate)
            {
                return RedirectToAction(nameof(Details), new { id });
            }
            // Get participating teams
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(id);
            // Build list of team names
            var teamNames = new List<string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    // Fallback to captain's username
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                teamNames.Add(name);
            }
            List<List<MatchInfo>> rounds;
            if (teamNames.Count < 2)
            {
                rounds = new List<List<MatchInfo>>();
            }
            else
            {
                rounds = GenerateBracketRounds(teamNames);
            }
            var model = new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
            return View(model);
        }

        /// <summary>
        /// Generates a tournament bracket for the given list of team names.
        /// Creates pairs for each round, automatically advancing teams when
        /// there are byes.  The algorithm calculates the next power of two
        /// greater than or equal to the number of teams to determine the
        /// bracket size.
        /// </summary>
        private List<List<MatchInfo>> GenerateBracketRounds(List<string> teams)
        {
            // Shuffle teams randomly
            var random = new Random();
            var shuffled = teams.OrderBy(_ => random.Next()).ToList();
            int n = shuffled.Count;
            // Determine bracket size (next power of two)
            int rounds = (int)Math.Ceiling(Math.Log2(n));
            int bracketSize = (int)Math.Pow(2, rounds);
            // Fill slots with teams and null for byes
            var slots = new string?[bracketSize];
            for (int i = 0; i < bracketSize; i++)
            {
                slots[i] = i < n ? shuffled[i] : null;
            }
            var result = new List<List<MatchInfo>>();
            // Initialize first round
            var firstRoundMatches = new List<MatchInfo>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var match = new MatchInfo { Team1Name = slots[i], Team2Name = slots[i + 1] };
                firstRoundMatches.Add(match);
            }
            result.Add(firstRoundMatches);
            // Compute winners for next rounds and build match pairs
            var winners = new List<string?>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var t1 = slots[i];
                var t2 = slots[i + 1];
                if (t1 != null && t2 == null)
                {
                    winners.Add(t1);
                }
                else if (t2 != null && t1 == null)
                {
                    winners.Add(t2);
                }
                else
                {
                    winners.Add(null);
                }
            }
            // Build subsequent rounds
            for (int r = 1; r < rounds; r++)
            {
                var roundMatches = new List<MatchInfo>();
                var nextWinners = new List<string?>();
                for (int i = 0; i < winners.Count; i += 2)
                {
                    var match = new MatchInfo
                    {
                        Team1Name = winners[i],
                        Team2Name = (i + 1 < winners.Count) ? winners[i + 1] : null
                    };
                    roundMatches.Add(match);
                    // Determine automatic winner for next round
                    var w1 = winners[i];
                    var w2 = (i + 1 < winners.Count) ? winners[i + 1] : null;
                    if (w1 != null && w2 == null)
                    {
                        nextWinners.Add(w1);
                    }
                    else if (w2 != null && w1 == null)
                    {
                        nextWinners.Add(w2);
                    }
                    else
                    {
                        nextWinners.Add(null);
                    }
                }
                result.Add(roundMatches);
                winners = nextWinners;
            }
            return result;
        }

        /// <summary>
        /// Displays a list of upcoming tournaments.  An optional format filter
        /// restricts results to tournaments with a specific team size.
        /// </summary>
        public async Task<IActionResult> Index(string? format)
        {
            var tournaments = await _tournamentsRepository.GetUpcomingTournamentsAsync(format);
            ViewBag.SelectedFormat = format;
            ViewBag.IsMy = false;
            return View(tournaments);
        }

        /// <summary>
        /// Shows tournaments associated with the currently logged‑in user.  If
        /// there is no join table, tournaments created by the user are displayed.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> MyTournaments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tournaments = await _tournamentsRepository.GetUserTournamentsAsync(userId);
            ViewBag.SelectedFormat = null;
            ViewBag.IsMy = true;
            return View("Index", tournaments);
        }

        /// <summary>
        /// Returns the view for creating a new tournament.  Only organizers and administrators have access.
        /// </summary>
        [Authorize(Roles = "Organizer,Admin")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Handles POST requests for creating a new tournament.  On successful validation a new tournament is added to the repository
        /// and the user is redirected back to the index.  Only organizers and administrators can create tournaments.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTournament(Tournament tournament)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                tournament.CreatedByUserId = user.Id;
                // Generate join code for private tournaments
                if (!tournament.IsPublic)
                {
                    tournament.JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                }
                await _tournamentsRepository.AddTournamentAsync(tournament);
                return RedirectToAction(nameof(Index));
            }
            return View(tournament);
        }

        /// <summary>
        /// Displays details of a specific tournament.  Only authenticated users can access tournament details.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            var viewModel = new TournamentDetailsViewModel { Tournament = tournament };
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            if (!string.IsNullOrEmpty(userId))
            {
                // Retrieve user's team using repository
                var team = await _tournamentsRepository.GetUserTeamAsync(id, userId);
                if (team != null)
                {
                    viewModel.UserTeam = team;
                    var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
                    foreach (var uid in memberIds)
                    {
                        var user = await _userManager.FindByIdAsync(uid);
                        if (user != null)
                        {
                            viewModel.TeamMembers.Add(user.UserName);
                        }
                    }
                }
            }
            return View(viewModel);
        }

        /// <summary>
        /// Handles joining a tournament.  If the tournament is premium, checks the user's subscription.
        /// If the tournament format is 1v1 the user simply creates a single‑player team.  For larger formats a
        /// team is created and an invitation link is generated via TempData so that the user can invite friends.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Jeśli turniej jest premium, upewnij się że użytkownik posiada aktywną subskrypcję z dostępem premium
            if (tournament.IsPremium)
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
                if (subscription == null || !subscription.Plan.IsPremiumAccess)
                {
                    TempData["SubscriptionError"] = "Ten turniej jest dostępny tylko dla użytkowników z pakietem Premium lub Ultra.";
                    return RedirectToAction("Index", "Subscription");
                }
            }
            // Check if the user already belongs to a team in this tournament
            var existingTeam = await _tournamentsRepository.GetUserTeamAsync(id, userId);
            if (existingTeam != null)
            {
                // Already joined
                return RedirectToAction(nameof(Details), new { id });
            }
            // Create a new team and add the current user as captain
            var team = await _tournamentsRepository.CreateTeamWithCaptainAsync(id, userId);
            // Generate invitation link for team tournaments (more than one player)
            if (tournament.Format != "1v1")
            {
                var inviteLink = Url.Action(nameof(JoinTeam), nameof(TournamentsController).Replace("Controller", string.Empty), new { teamId = team.Id, code = team.JoinCode }, Request.Scheme);
                TempData["InviteLink"] = inviteLink;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Allows a user to join an existing team via an invitation link.  The code must match the team's join code.
        /// If the code is invalid or the team does not exist, a 404 is returned.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> JoinTeam(int teamId, string code)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tournamentId = await _tournamentsRepository.AddUserToTeamAsync(teamId, userId, code);
            if (!tournamentId.HasValue)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Details), new { id = tournamentId.Value });
        }
    }
}