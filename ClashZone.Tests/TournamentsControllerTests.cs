using System.Threading.Tasks;
using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using ClashZone.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="TournamentsController"/> — reguły (własne vs domyślne),
    /// walidacja formatu per gra oraz obsługa braku turnieju.
    /// </summary>
    public class TournamentsControllerTests
    {
        private static TournamentsController CreateController(
            Mock<ITournamentService>? service = null,
            Mock<ITournamentsRepository>? repo = null,
            Mock<IMapper>? mapper = null)
        {
            return new TournamentsController(
                TestSupport.MockUserManager().Object,
                new Mock<ISubscriptionRepository>().Object,
                new Mock<IChatService>().Object,
                new Mock<IBracketService>().Object,
                (service ?? new Mock<ITournamentService>()).Object,
                (repo ?? new Mock<ITournamentsRepository>()).Object,
                new Mock<IEmailService>().Object,
                new Mock<IMatchesService>().Object,
                (mapper ?? new Mock<IMapper>()).Object);
        }

        [Fact]
        public async Task Rules_UsesCustomRules_WhenProvided()
        {
            var repo = new Mock<ITournamentsRepository>();
            repo.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Cs2, Rules = "Reguła A\nReguła B" });
            var controller = CreateController(repo: repo);

            var result = await controller.Rules(1);

            var model = Assert.IsType<RulesViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(new[] { "Reguła A", "Reguła B" }, model.Rules.ToArray());
        }

        [Fact]
        public async Task Rules_FallsBackToGameDefaults_WhenEmpty()
        {
            var repo = new Mock<ITournamentsRepository>();
            repo.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Lol, Rules = null });
            var controller = CreateController(repo: repo);

            var result = await controller.Rules(1);

            var model = Assert.IsType<RulesViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(GameCatalog.DefaultRules(GameCatalog.Lol), model.Rules);
        }

        [Fact]
        public async Task Rules_ReturnsNotFound_WhenTournamentMissing()
        {
            var repo = new Mock<ITournamentsRepository>();
            repo.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync((TournamentDomain?)null);

            var result = await CreateController(repo: repo).Rules(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateTournament_InvalidFormatForGame_ReturnsCreateViewWithError()
        {
            var service = new Mock<ITournamentService>();
            var controller = CreateController(service: service);
            TestSupport.SetUser(controller, "org1", roles: "Organizer");

            // League of Legends nie dopuszcza formatu 2v2.
            var model = new TournamentFormViewModel { Name = "Cup", GameTitle = GameCatalog.Lol, Format = "2v2" };
            var result = await controller.CreateTournament(model, null, null, null);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", view.ViewName);
            Assert.False(controller.ModelState.IsValid);
            service.Verify(s => s.CreateTournamentAsync(It.IsAny<TournamentDomain>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateTournament_Valid_CreatesAndRedirects()
        {
            var service = new Mock<ITournamentService>();
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<TournamentDomain>(It.IsAny<TournamentFormViewModel>()))
                .Returns(new TournamentDomain());
            var controller = CreateController(service: service, mapper: mapper);
            TestSupport.SetUser(controller, "org1", roles: "Organizer");

            var model = new TournamentFormViewModel { Name = "Cup", GameTitle = GameCatalog.Lol, Format = "5v5" };
            var result = await controller.CreateTournament(model, null, null, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            service.Verify(s => s.CreateTournamentAsync(It.IsAny<TournamentDomain>(), "org1"), Times.Once);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenMissing()
        {
            var service = new Mock<ITournamentService>();
            service.Setup(s => s.GetTournamentDetailsAsync(1, It.IsAny<string?>())).ReturnsAsync((TournamentDetailsViewModel?)null);
            var controller = CreateController(service: service);
            TestSupport.SetUser(controller, null, isAuthenticated: false);

            var result = await controller.Details(1);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
