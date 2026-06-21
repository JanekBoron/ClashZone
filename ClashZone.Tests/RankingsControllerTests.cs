using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.ViewModels;
using ClashZone.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class RankingsControllerTests
    {
        private static UserStatDomain Stat(string id, int kills, int deaths, int assists = 0)
            => new UserStatDomain
            {
                UserId = id,
                TotalKills = kills,
                TotalDeaths = deaths,
                TotalAssists = assists,
                User = new ClashUserDomain { Id = id, UserName = id }
            };

        [Fact]
        public async Task Index_DefaultsToCs2_AndOrdersByAverageKdDescending()
        {
            var repo = new Mock<IPlayerStatsRepository>();
            repo.Setup(r => r.GetAllUserStatsAsync(GameCatalog.Cs2)).ReturnsAsync(new List<UserStatDomain>
            {
                Stat("u3", 8, 4),
                Stat("u1", 10, 2),
                Stat("u2", 3, 1)
            });

            var result = await new RankingsController(repo.Object).Index();

            var model = Assert.IsType<RankingViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(GameCatalog.Cs2, model.SelectedGame);
            Assert.Equal(new[] { "u1", "u2", "u3" }, model.Rows.Select(r => r.Stat.UserId).ToArray());
        }

        [Fact]
        public async Task Index_Lol_PopulatesFavoriteChampion()
        {
            var repo = new Mock<IPlayerStatsRepository>();
            repo.Setup(r => r.GetAllUserStatsAsync(GameCatalog.Lol))
                .ReturnsAsync(new List<UserStatDomain> { Stat("u1", 10, 2, 5) });
            repo.Setup(r => r.GetMostPlayedChampionAsync("u1", GameCatalog.Lol)).ReturnsAsync("Ahri");

            var result = await new RankingsController(repo.Object).Index(GameCatalog.Lol);

            var model = Assert.IsType<RankingViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(GameCatalog.Lol, model.SelectedGame);
            Assert.Equal("Ahri", Assert.Single(model.Rows).FavoriteChampion);
        }

        [Fact]
        public async Task Index_ClampsPage_ToValidRange()
        {
            var stats = Enumerable.Range(0, 15)
                .Select(i => Stat(i.ToString(), i + 1, 1))
                .ToList();
            var repo = new Mock<IPlayerStatsRepository>();
            repo.Setup(r => r.GetAllUserStatsAsync(GameCatalog.Cs2)).ReturnsAsync(stats);

            var result = await new RankingsController(repo.Object).Index(page: 5);

            var model = Assert.IsType<RankingViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(2, model.CurrentPage);
            Assert.Equal(2, model.TotalPages);
            Assert.Equal(5, model.Rows.Count);
        }

        [Fact]
        public async Task Index_Search_FiltersByPlayerName_CaseInsensitive()
        {
            var repo = new Mock<IPlayerStatsRepository>();
            repo.Setup(r => r.GetAllUserStatsAsync(GameCatalog.Cs2)).ReturnsAsync(new List<UserStatDomain>
            {
                Stat("alice", 10, 2),
                Stat("bob", 8, 4),
                Stat("alina", 6, 3)
            });

            var result = await new RankingsController(repo.Object).Index(search: "ALI");

            var model = Assert.IsType<RankingViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("ALI", model.SearchTerm);
            Assert.Equal(new[] { "alice", "alina" }, model.Rows.Select(r => r.Stat.UserId).OrderBy(x => x).ToArray());
        }

        [Fact]
        public async Task Index_Search_RecomputesPagination_OverFilteredResults()
        {
            var stats = Enumerable.Range(0, 15)
                .Select(i => Stat($"player{i}", i + 1, 1))
                .ToList();
            var repo = new Mock<IPlayerStatsRepository>();
            repo.Setup(r => r.GetAllUserStatsAsync(GameCatalog.Cs2)).ReturnsAsync(stats);

            var result = await new RankingsController(repo.Object).Index(search: "player1");

            var model = Assert.IsType<RankingViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(6, model.Rows.Count);
            Assert.Equal(1, model.TotalPages);
        }
    }
}
