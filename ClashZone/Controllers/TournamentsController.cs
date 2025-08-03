using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
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
        /// Lista nadchodzących turniejów.  Opcjonalnie można filtrować po formacie (1v1, 2v2, 5v5).
        /// </summary>
        public async Task<IActionResult> Index(string? format)
        {
            var tournaments = await _tournamentsRepository.GetUpcomingTournamentsAsync(format);
            ViewBag.SelectedFormat = format;
            ViewBag.IsMy = false;
            return View(tournaments);
        }

        /// <summary>
        /// Lista turniejów, w których bierze udział lub jest zapisany aktualny użytkownik.
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

        [Authorize(Roles = "Organizer,Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTournament(Tournament tournament)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                tournament.CreatedByUserId = user.Id;

                // Generuj kod tylko dla turniejów prywatnych
                if (!tournament.IsPublic)
                {
                    tournament.JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                }

                await _tournamentsRepository.AddTournamentAsync(tournament);
                return RedirectToAction(nameof(Index));
            }

            return View(tournament);
        }
    }
}
