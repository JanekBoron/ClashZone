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
using System.Security.Claims;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Handles actions related to tournaments, including listing upcoming
    /// tournaments, displaying tournaments created by the current user,
    /// creating new tournaments and showing detailed information about a
    /// specific tournament.
    /// </summary>
    public class TournamentsController : Controller
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly UserManager<ClashUser> _userManager;

        public TournamentsController(ITournamentsRepository tournamentsRepository, UserManager<ClashUser> userManager)
        {
            _tournamentsRepository = tournamentsRepository;
            _userManager = userManager;
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
        /// there is no join table, tournaments created by the user are
        /// displayed.
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
        /// Returns the view for creating a new tournament.  Only organizers
        /// and administrators have access.
        /// </summary>
        [Authorize(Roles = "Organizer,Admin")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Handles POST requests for creating a new tournament.  On
        /// successful validation a new tournament is added to the repository
        /// and the user is redirected back to the index.  Only organizers and
        /// administrators can create tournaments.
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
        /// Displays details of a specific tournament.  Only authenticated
        /// users can access tournament details.
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
        /// Handles joining a tournament.  If the tournament format is 1v1 the
        /// user simply creates a single‑player team.  For larger formats a
        /// team is created and an invitation link is generated via
        /// <see cref="TempData"/> so that the user can invite friends.  This
        /// action requires the user to be authenticated.
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
        /// Allows a user to join an existing team via an invitation link.  The
        /// <paramref name="code"/> must match the team's join code.  If the
        /// code is invalid or the team does not exist, a 404 is returned.
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
