using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;              
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services;
using ClashZone.ViewModels;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;


namespace ClashZone.Tests
{
    /// <summary>
    /// Unit tests for <see cref="TournamentService"/>.  These tests
    /// verify that the service forwards calls to the underlying
    /// repository appropriately, sets defaults on entities and handles
    /// various edge cases when joining tournaments.
    /// </summary>
    public class TournamentServiceTests
    {
        private static UserManager<ClashUser> CreateUserManager()
        {
            var store = new Mock<IUserStore<ClashUser>>();
            var userValidators = new List<IUserValidator<ClashUser>>();
            var pwdValidators = new List<IPasswordValidator<ClashUser>>();
            return new UserManager<ClashUser>(store.Object,
                null, null, userValidators, pwdValidators, null, null, null, null);
        }

        [Fact]
        public async Task GetUpcomingTournaments_ForwardsToRepository()
        {
            // Arrange
            var tournaments = new List<Tournament> { new Tournament(), new Tournament() };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetUpcomingTournamentsAsync(null))
                .ReturnsAsync(tournaments);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.GetUpcomingTournamentsAsync(null);

            // Assert
            Assert.Equal(tournaments, result);
        }

        [Fact]
        public async Task GetUserTournaments_ForwardsToRepository()
        {
            // Arrange
            var list = new List<Tournament> { new Tournament { Id = 1 } };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetUserTournamentsAsync("user"))
                .ReturnsAsync(list);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.GetUserTournamentsAsync("user");

            // Assert
            Assert.Equal(list, result);
        }

        [Fact]
        public async Task CreateTournamentAsync_SetsCreatedByUserId_AndAddsTournament()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            Tournament? captured = null;
            repoMock.Setup(r => r.AddTournamentAsync(It.IsAny<Tournament>()))
                .Callback<Tournament>(t => captured = t)
                .Returns(Task.CompletedTask);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());
            var tournament = new Tournament { IsPublic = false };

            // Act
            await service.CreateTournamentAsync(tournament, "creator");

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("creator", captured!.CreatedByUserId);
            Assert.False(string.IsNullOrEmpty(captured.JoinCode));
        }

        [Fact]
        public async Task GetTournamentDetails_ReturnsNull_WhenTournamentNotFound()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync((Tournament?)null);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.GetTournamentDetailsAsync(1, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTournamentDetails_ReturnsViewModel_WithUserTeamDetails()
        {
            // Arrange
            int tournamentId = 1;
            string userId = "user";
            var tournament = new Tournament { Id = tournamentId };
            var team = new Team { Id = 2 };
            var memberIds = new List<string> { "user", "other" };
            var users = new Dictionary<string, ClashUser>
            {
                { "user", new ClashUser { Id = "user", UserName = "User" } },
                { "other", new ClashUser { Id = "other", UserName = "Other" } }
            };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(tournamentId))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetUserTeamAsync(tournamentId, userId))
                .ReturnsAsync(team);
            repoMock.Setup(r => r.GetTeamMemberIdsAsync(team.Id))
                .ReturnsAsync(memberIds);
            var userManager = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => users[id]);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), userManager.Object);

            // Act
            var result = await service.GetTournamentDetailsAsync(tournamentId, userId);

            // Assert
            var model = Assert.IsType<TournamentDetailsViewModel>(result);
            Assert.Equal(tournament, model.Tournament);
            Assert.Equal(team, model.UserTeam);
            Assert.Equal(new[] { "User", "Other" }, model.TeamMembers);
        }

        [Fact]
        public async Task JoinTournament_ReturnsNotFound_WhenTournamentNull()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync((Tournament?)null);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.JoinTournamentAsync(1, "user");

            // Assert
            Assert.True(result.NotFound);
        }

        [Fact]
        public async Task JoinTournament_ReturnsMaxParticipantsExceeded_WhenFull()
        {
            // Arrange
            var tournament = new Tournament { Id = 1, MaxParticipants = 1 };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(new List<Team> { new Team() });
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.JoinTournamentAsync(1, "user");

            // Assert
            Assert.True(result.MaxParticipantsExceeded);
        }

        [Fact]
        public async Task JoinTournament_ReturnsRequiresSubscription_WhenPremiumAndNoSubscription()
        {
            // Arrange
            var tournament = new Tournament { Id = 1, IsPremium = true };

            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                    .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(1))
                    .ReturnsAsync(new List<Team>());

            var subscriptionRepoMock = new Mock<ISubscriptionRepository>();

            subscriptionRepoMock
                .Setup(s => s.GetActiveSubscriptionAsync("user"))
                .ReturnsAsync((UserSubscription?)null);


            var service = new TournamentService(
                repoMock.Object,
                subscriptionRepoMock.Object,
                CreateUserManager());  

            // Act
            var result = await service.JoinTournamentAsync(1, "user");

            // Assert
            Assert.True(result.RequiresSubscription);
        }

        [Fact]
        public async Task JoinTournament_ReturnsAlreadyJoined_WhenUserHasTeam()
        {
            // Arrange
            var tournament = new Tournament { Id = 1 };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(new List<Team>());
            repoMock.Setup(r => r.GetUserTeamAsync(1, "user"))
                .ReturnsAsync(new Team());
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.JoinTournamentAsync(1, "user");

            // Assert
            Assert.True(result.AlreadyJoined);
        }

        [Fact]
        public async Task JoinTournament_ReturnsTeamAndFormat_WhenSuccessful()
        {
            // Arrange
            var tournament = new Tournament { Id = 1, Format = "2v2", IsPremium = false };
            var team = new Team { Id = 10, JoinCode = "ABC" };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(new List<Team>());
            repoMock.Setup(r => r.GetUserTeamAsync(1, "user"))
                .ReturnsAsync((Team?)null);
            repoMock.Setup(r => r.CreateTeamWithCaptainAsync(1, "user"))
                .ReturnsAsync(team);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.JoinTournamentAsync(1, "user");

            // Assert
            Assert.Equal(team, result.Team);
            Assert.Equal("2v2", result.TournamentFormat);
            Assert.Equal(false, result.IsPremium);
        }

        [Fact]
        public async Task JoinTeam_ForwardsToRepository()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.AddUserToTeamAsync(5, "user", "CODE"))
                .ReturnsAsync(1);
            var service = new TournamentService(repoMock.Object, Mock.Of<ISubscriptionRepository>(), CreateUserManager());

            // Act
            var result = await service.JoinTeamAsync(5, "user", "CODE");

            // Assert
            Assert.Equal(1, result);
        }
    }
}