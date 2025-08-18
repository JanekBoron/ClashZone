using ClashZone.DataAccess;
using ClashZone.DataAccess.Models;
using ClashZone.ViewModels;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClashZone.Controllers
{
    /// <summary>
    /// Controller responsible for displaying player rankings.  Rankings are computed
    /// using the players' average kill/death ratio (K/D) which is pre‑aggregated in
    /// the UserStat entity.  This controller retrieves all statistics, orders
    /// them descending by AverageKD and paginates the results.  Only authenticated
    /// users can access the rankings via the navigation bar.
    /// </summary>
    public class RankingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Default number of entries displayed per page.
        /// </summary>
        private const int DefaultPageSize = 10;

        public RankingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Displays the rankings page.  The optional page parameter determines
        /// which subset of players is shown.  Results are sorted by AverageKD
        /// in descending order.
        /// </summary>
        /// <param name="page">Page number starting at 1.</param>
        /// <returns>The rankings view.</returns>
        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            // Load all user statistics into memory to perform ordering on the
            // computed AverageKD property.  Since AverageKD is a non‑mapped
            // property, Entity Framework cannot translate it to SQL.  By
            // materializing the list we can perform the ordering in memory.
            var allStats = await _context.UserStats
                .Include(us => us.User)
                .ToListAsync();

            // Order descending by AverageKD on the client side
            var orderedStats = allStats
                .OrderByDescending(us => us.AverageKD)
                .ToList();

            // Determine total number of pages based on the number of records
            int totalCount = orderedStats.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize);

            // Clamp page to a valid range
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            // Fetch a page of results
            var stats = orderedStats
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

            // Build view model
            var model = new RankingViewModel
            {
                UserStats = stats,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = DefaultPageSize
            };

            return View(model);
        }
    }
}