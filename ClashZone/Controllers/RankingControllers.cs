using ClashZone.App.Domains;
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

        public async Task<IActionResult> Index(string? game = null, int page = 1, string? search = null)
        {
            var selectedGame = GameCatalog.Normalize(game) ?? GameCatalog.Cs2;

            if (page < 1)
            {
                page = 1;
            }

            var allStats = await _playerStatsRepository.GetAllUserStatsAsync(selectedGame);

            var orderedStats = allStats
                .OrderByDescending(us => GameCatalog.IsLol(selectedGame) ? us.AverageKDA : us.AverageKD)
                .ToList();

            var searchTerm = search?.Trim();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                orderedStats = orderedStats
                    .Where(us =>
                        (us.User.DisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (us.User.UserName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            int totalCount = orderedStats.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var pageStats = orderedStats
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

            var rows = new List<RankingRowViewModel>();
            foreach (var stat in pageStats)
            {
                string? favoriteChampion = null;
                if (GameCatalog.IsLol(selectedGame))
                {
                    favoriteChampion = await _playerStatsRepository.GetMostPlayedChampionAsync(stat.UserId, selectedGame);
                }
                rows.Add(new RankingRowViewModel { Stat = stat, FavoriteChampion = favoriteChampion });
            }

            var model = new RankingViewModel
            {
                Rows = rows,
                SelectedGame = selectedGame,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = DefaultPageSize,
                SearchTerm = searchTerm
            };

            return View(model);
        }
    }
}
