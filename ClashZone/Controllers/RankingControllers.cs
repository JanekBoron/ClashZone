using ClashZone.App.Ports.DataAccess;
using ClashZone.App.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ClashZone.Controllers
{
    public class RankingsController : Controller
    {
        private readonly IPlayerStatsRepository _playerStatsRepository;
        private const int DefaultPageSize = 10;

        public RankingsController(IPlayerStatsRepository playerStatsRepository)
        {
            _playerStatsRepository = playerStatsRepository;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            var allStats = await _playerStatsRepository.GetAllUserStatsAsync();

            var orderedStats = allStats
                .OrderByDescending(us => us.AverageKD)
                .ToList();

            int totalCount = orderedStats.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var stats = orderedStats
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

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
