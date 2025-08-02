using ClashZone.DataAccess.Repository.Interfaces;
using DataAccess;
using DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace ClashZone.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ITournamentsRepository _tournamentsRepository;

        public TournamentsController(ITournamentsRepository tournamentsRepository)
        {
            _tournamentsRepository = tournamentsRepository;
        }

        public async Task<IActionResult> Index()
        {
            var tournaments = await _tournamentsRepository.GetUpcomingTournamentsAsync();
            return View(tournaments);
        }
    }
}
