using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using DataAccess;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Unit tests for <see cref="BracketService"/>.  These tests focus on
    /// the early exit conditions of the service: when a tournament cannot
    /// be found or when there are fewer than two teams.  More complex
    /// bracket generation is covered by integration tests elsewhere.
    /// </summary>
    public class BracketServiceTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "BracketServiceTests")
                .Options;
            return new ApplicationDbContext(options);
        }

        private static UserManager<ClashUser> CreateUserManager()
        {
            var store = new Mock<IUserStore<ClashUser>>();
            return new UserManager<ClashUser>(store.Object,
                null, null, new List<IUserValidator<ClashUser>>(), new List<IPasswordValidator<ClashUser>>(), null, null, null, null);
        }

        [Fact]
        public async Task GetBracketAsync_ReturnsNull_WhenTournamentNotFound()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync((Tournament?)null);
            var service = new BracketService(repoMock.Object, CreateUserManager(), Mock.Of<IEmailService>(), CreateContext(), Mock.Of<ICoinWalletService>());

            // Act
            var result = await service.GetBracketAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBracketAsync_ReturnsEmptyRounds_WhenNotEnoughTeams()
        {
            // Arrange
            var tournament = new Tournament { Id = 1 };
            var teams = new List<Team> { new Team { Id = 1, CaptainId = "u1" } };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(teams);
            var userManager = CreateUserManager();
            var service = new BracketService(repoMock.Object, userManager, Mock.Of<IEmailService>(), CreateContext(), Mock.Of<ICoinWalletService>());

            // Act
            var result = await service.GetBracketAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tournament, result!.Tournament);
            Assert.Empty(result.Rounds);
        }

        [Fact]
        public async Task GetBracketWithResultsAsync_ReturnsEmptyRounds_WhenNotEnoughTeams()
        {
            // Arrange
            var tournament = new Tournament { Id = 1 };
            var teams = new List<Team> { new Team { Id = 1, CaptainId = "u1" } };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(teams);
            var userManager = CreateUserManager();
            var context = CreateContext();
            var service = new BracketService(repoMock.Object, userManager, Mock.Of<IEmailService>(), context, Mock.Of<ICoinWalletService>());

            // Act
            var result = await service.GetBracketWithResultsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tournament, result!.Tournament);
            Assert.Empty(result.Rounds);
        }

        [Fact]
        public async Task GetBracketWithStatsAsync_ReturnsEmptyRounds_WhenNotEnoughTeams()
        {
            // Arrange
            var tournament = new Tournament { Id = 1 };
            var teams = new List<Team> { new Team { Id = 1, CaptainId = "u1" } };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(teams);
            var userManager = CreateUserManager();
            var context = CreateContext();
            var service = new BracketService(repoMock.Object, userManager, Mock.Of<IEmailService>(), context, Mock.Of<ICoinWalletService>());

            // Act
            var result = await service.GetBracketWithStatsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tournament, result!.Tournament);
            Assert.Empty(result.Rounds);
        }
    }
}