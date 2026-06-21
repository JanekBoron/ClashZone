using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Ports.Games;
using ClashZone.App.ViewModels;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class ProfileControllerTests
    {
        [Fact]
        public async Task Index_BuildsPerGameStatistics_AndFavoriteChampion()
        {
            var user = new ClashUser { Id = "u1", UserName = "alice" };
            var um = TestSupport.MockUserManager();
            um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var sm = TestSupport.MockSignInManager(um.Object);

            var subs = new Mock<ISubscriptionRepository>();
            subs.Setup(s => s.GetActiveSubscriptionAsync("u1")).ReturnsAsync((UserSubscriptionDomain?)null);

            var stats = new Mock<IPlayerStatsRepository>();
            stats.Setup(s => s.GetUserStatsAsync("u1", GameCatalog.Cs2))
                .ReturnsAsync(new UserStatDomain { UserId = "u1", Game = GameCatalog.Cs2, MatchesPlayed = 3 });
            stats.Setup(s => s.GetUserStatsAsync("u1", GameCatalog.Lol))
                .ReturnsAsync(new UserStatDomain { UserId = "u1", Game = GameCatalog.Lol, MatchesPlayed = 5 });
            stats.Setup(s => s.GetMostPlayedChampionAsync("u1", GameCatalog.Lol)).ReturnsAsync("Ahri");

            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<ClashUserDomain>(It.IsAny<ClashUser>()))
                .Returns(new ClashUserDomain { Id = "u1", UserName = "alice" });

            var controller = new ProfileController(
                um.Object, sm.Object, subs.Object, stats.Object,
                Mock.Of<IWebHostEnvironment>(), Mock.Of<ICoinWalletRepository>(),
                Mock.Of<IProductRedeemRepository>(), mapper.Object, Mock.Of<IRiotApiClient>());
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Index();

            var model = Assert.IsType<ProfileViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.NotNull(model.Cs2Statistics);
            Assert.NotNull(model.LolStatistics);
            Assert.Equal("Ahri", model.LolFavoriteChampion);
        }

        [Fact]
        public async Task Orders_ReturnsUsersRedeemOrders()
        {
            var redemptions = new Mock<IProductRedeemRepository>();
            redemptions.Setup(r => r.GetByUserAsync("u1")).ReturnsAsync(new List<ProductRedeemDomain>
            {
                new() { Id = 1, UserId = "u1", Status = OrderStatus.Pending, Product = new ProductDomain { Name = "Cap" } }
            });

            var controller = new ProfileController(
                TestSupport.MockUserManager().Object,
                TestSupport.MockSignInManager(TestSupport.MockUserManager().Object).Object,
                Mock.Of<ISubscriptionRepository>(),
                Mock.Of<IPlayerStatsRepository>(),
                Mock.Of<IWebHostEnvironment>(),
                Mock.Of<ICoinWalletRepository>(),
                redemptions.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IRiotApiClient>());
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Orders();

            var model = Assert.IsAssignableFrom<IEnumerable<ProductRedeemDomain>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model);
        }
    }
}
