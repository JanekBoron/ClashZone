using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class TournamentServiceTests
    {
        private static TournamentService CreateService(
            Mock<ITournamentsRepository> tournaments,
            Mock<ISubscriptionRepository>? subs = null,
            Mock<IUserRepository>? users = null)
            => new TournamentService(
                tournaments.Object,
                (subs ?? new Mock<ISubscriptionRepository>()).Object,
                (users ?? new Mock<IUserRepository>()).Object);

        [Fact]
        public async Task CreateTournamentAsync_SetsCreator_AndGeneratesJoinCodeForPrivate()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            TournamentDomain? saved = null;
            tournaments.Setup(r => r.AddTournamentAsync(It.IsAny<TournamentDomain>()))
                .Callback<TournamentDomain>(t => saved = t)
                .Returns(Task.CompletedTask);

            var tournament = new TournamentDomain { Name = "Cup", IsPublic = false };
            await CreateService(tournaments).CreateTournamentAsync(tournament, "user-1");

            Assert.NotNull(saved);
            Assert.Equal("user-1", saved!.CreatedByUserId);
            Assert.False(string.IsNullOrEmpty(saved.JoinCode));
            tournaments.Verify(r => r.AddTournamentAsync(tournament), Times.Once);
        }

        [Fact]
        public async Task GetTournamentDetailsAsync_ReturnsNull_WhenNotFound()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(99)).ReturnsAsync((TournamentDomain?)null);

            Assert.Null(await CreateService(tournaments).GetTournamentDetailsAsync(99, "u1"));
        }

        [Fact]
        public async Task GetTournamentDetailsAsync_PopulatesUserTeamAndMembers()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1, Name = "Cup", Format = "2v2" });
            tournaments.Setup(r => r.GetUserTeamAsync(1, "u1")).ReturnsAsync(new TeamDomain { Id = 10, CaptainId = "u1" });
            tournaments.Setup(r => r.GetTeamMembersAsync(10)).ReturnsAsync(new List<TeamMemberDomain>
            {
                new() { TeamId = 10, UserId = "u1", IsReady = true },
                new() { TeamId = 10, UserId = "u2", IsReady = false }
            });

            var users = new Mock<IUserRepository>();
            users.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ClashUserDomain { Id = id, UserName = "name_" + id });

            var model = await CreateService(tournaments, users: users).GetTournamentDetailsAsync(1, "u1");

            Assert.NotNull(model);
            Assert.NotNull(model!.UserTeam);
            Assert.Equal(new[] { "name_u1", "name_u2" }, model.Members.Select(m => m.Name).ToArray());
            Assert.True(model.IsCaptain);
            Assert.Equal(2, model.RequiredRosterSize);
        }

        [Fact]
        public async Task JoinTournamentAsync_ReturnsNotFound_WhenMissing()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync((TournamentDomain?)null);

            var result = await CreateService(tournaments).JoinTournamentAsync(1, "u1");

            Assert.True(result.NotFound);
        }

        [Fact]
        public async Task JoinTournamentAsync_ReturnsMaxExceeded_WhenFull()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(new TournamentDomain { Id = 1, MaxParticipants = 2 });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(new List<TeamDomain> { new(), new() });

            var result = await CreateService(tournaments).JoinTournamentAsync(1, "u1");

            Assert.True(result.MaxParticipantsExceeded);
        }

        [Fact]
        public async Task JoinTournamentAsync_RequiresSubscription_WhenPremiumWithoutPlan()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(new TournamentDomain { Id = 1, MaxParticipants = 16, IsPremium = true });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1)).ReturnsAsync(new List<TeamDomain>());

            var subs = new Mock<ISubscriptionRepository>();
            subs.Setup(s => s.GetActiveSubscriptionAsync("u1")).ReturnsAsync((UserSubscriptionDomain?)null);

            var result = await CreateService(tournaments, subs).JoinTournamentAsync(1, "u1");

            Assert.True(result.RequiresSubscription);
        }

        [Fact]
        public async Task JoinTournamentAsync_ReturnsAlreadyJoined_WhenUserHasTeam()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(new TournamentDomain { Id = 1, MaxParticipants = 16 });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1)).ReturnsAsync(new List<TeamDomain>());
            tournaments.Setup(r => r.GetUserTeamAsync(1, "u1")).ReturnsAsync(new TeamDomain { Id = 7, CaptainId = "u1" });

            var result = await CreateService(tournaments).JoinTournamentAsync(1, "u1");

            Assert.True(result.AlreadyJoined);
        }

        [Fact]
        public async Task JoinTournamentAsync_CreatesTeam_OnSuccess()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(new TournamentDomain { Id = 1, MaxParticipants = 16, Format = "5v5" });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1)).ReturnsAsync(new List<TeamDomain>());
            tournaments.Setup(r => r.GetUserTeamAsync(1, "u1")).ReturnsAsync((TeamDomain?)null);
            tournaments.Setup(r => r.CreateTeamWithCaptainAsync(1, "u1"))
                .ReturnsAsync(new TeamDomain { Id = 42, CaptainId = "u1" });

            var result = await CreateService(tournaments).JoinTournamentAsync(1, "u1");

            Assert.False(result.NotFound);
            Assert.False(result.AlreadyJoined);
            Assert.NotNull(result.Team);
            Assert.Equal(42, result.Team!.Id);
            Assert.Equal("5v5", result.TournamentFormat);
        }
    }
}
