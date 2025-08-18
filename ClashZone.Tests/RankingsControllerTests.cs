using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using ClashZone.ViewModels;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Tests for <see cref="RankingsController"/>.  The controller orders
    /// players by their average kill/death ratio and paginates the results.
    /// These tests verify ordering and page calculations using an in-memory
    /// database context.
    /// </summary>
    public class RankingsControllerTests
    {
        private static ApplicationDbContext CreateContext(IEnumerable<UserStat> stats)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.UserStats.AddRange(stats);
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task Index_OrdersStatsDescending_ByAverageKd()
        {
            // Arrange: three players with different AverageKD values
            var stats = new[]
            {
                new UserStat { UserId = "1", TotalKills = 10, TotalDeaths = 2, User = new ClashUser { Id = "1", UserName = "A" } }, // KD=5
                new UserStat { UserId = "2", TotalKills = 3, TotalDeaths = 1, User = new ClashUser { Id = "2", UserName = "B" } },   // KD=3
                new UserStat { UserId = "3", TotalKills = 8, TotalDeaths = 4, User = new ClashUser { Id = "3", UserName = "C" } }    // KD=2
            };
            using var context = CreateContext(stats);
            var controller = new RankingsController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RankingViewModel>(view.Model);
            Assert.Equal(3, model.UserStats.Count);
            // Validate ordering descending by AverageKD
            var ordered = model.UserStats.Select(s => s.UserId).ToList();
            Assert.Equal(new[] { "1", "2", "3" }, ordered);
            Assert.Equal(1, model.CurrentPage);
            Assert.Equal(1, model.TotalPages);
            Assert.Equal(10, model.PageSize);
        }

        [Fact]
        public async Task Index_ClampsPage_ToValidRange()
        {
            // Arrange: create 15 stats to have two pages (page size 10)
            var list = new List<UserStat>();
            for (int i = 0; i < 15; i++)
            {
                list.Add(new UserStat { UserId = i.ToString(), TotalKills = i + 1, TotalDeaths = 1, User = new ClashUser { Id = i.ToString(), UserName = $"User{i}" } });
            }
            using var context = CreateContext(list);
            var controller = new RankingsController(context);

            // Act: request page beyond total pages
            var result = await controller.Index(page: 5);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RankingViewModel>(view.Model);
            // Only two pages exist; request for page 5 should clamp to page 2
            Assert.Equal(2, model.CurrentPage);
            Assert.Equal(2, model.TotalPages);
            Assert.Equal(5, model.UserStats.Count); // second page has 5 items
        }
    }
}