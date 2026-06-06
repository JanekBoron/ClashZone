using System;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="LeagueTeamService"/> — dołączanie linkiem/kodem oraz
    /// egzekwowanie okresów karencji (cooldown).
    /// </summary>
    public class LeagueTeamServiceTests
    {
        private static LeagueTeamService CreateService(
            Mock<ILeagueTeamsRepository> teams,
            Mock<IUserRepository>? users = null,
            Mock<ITournamentsRepository>? tournaments = null)
            => new LeagueTeamService(
                teams.Object,
                (users ?? new Mock<IUserRepository>()).Object,
                (tournaments ?? new Mock<ITournamentsRepository>()).Object);

        private static void SetupTeam(Mock<ILeagueTeamsRepository> teams, int id = 1, string code = "ABC123")
            => teams.Setup(r => r.GetByIdAsync(id))
                    .ReturnsAsync(new LeagueTeamDomain { Id = id, JoinCode = code, CaptainId = "cap", Game = "Counter Strike 2" });

        [Fact]
        public async Task JoinByCodeAsync_ReturnsInvalidCode_WhenCodeMismatch()
        {
            var teams = new Mock<ILeagueTeamsRepository>();
            SetupTeam(teams);

            var result = await CreateService(teams).JoinByCodeAsync(1, "WRONG", "u1");

            Assert.True(result.InvalidCode);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task JoinByCodeAsync_ReturnsAlreadyMember_WhenMember()
        {
            var teams = new Mock<ILeagueTeamsRepository>();
            SetupTeam(teams);
            teams.Setup(r => r.IsMemberAsync(1, "u1")).ReturnsAsync(true);

            var result = await CreateService(teams).JoinByCodeAsync(1, "ABC123", "u1");

            Assert.True(result.AlreadyMember);
        }

        [Fact]
        public async Task JoinByCodeAsync_BlocksWhenUserJoinedAnotherTeamWithinWeek()
        {
            var teams = new Mock<ILeagueTeamsRepository>();
            SetupTeam(teams);
            teams.Setup(r => r.IsMemberAsync(1, "u1")).ReturnsAsync(false);
            teams.Setup(r => r.UserJoinedAnyTeamSinceAsync("u1", It.IsAny<DateTime>())).ReturnsAsync(true);

            var result = await CreateService(teams).JoinByCodeAsync(1, "ABC123", "u1");

            Assert.True(result.UserCooldown);
            teams.Verify(r => r.AddMemberAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task JoinByCodeAsync_BlocksWhenRosterReachedThreeJoinsThisWeek()
        {
            var teams = new Mock<ILeagueTeamsRepository>();
            SetupTeam(teams);
            teams.Setup(r => r.IsMemberAsync(1, "u1")).ReturnsAsync(false);
            teams.Setup(r => r.UserJoinedAnyTeamSinceAsync("u1", It.IsAny<DateTime>())).ReturnsAsync(false);
            teams.Setup(r => r.CountMembersJoinedSinceAsync(1, It.IsAny<DateTime>())).ReturnsAsync(3);

            var result = await CreateService(teams).JoinByCodeAsync(1, "ABC123", "u1");

            Assert.True(result.RosterCooldown);
            teams.Verify(r => r.AddMemberAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task JoinByCodeAsync_AddsMember_OnSuccess()
        {
            var teams = new Mock<ILeagueTeamsRepository>();
            SetupTeam(teams);
            teams.Setup(r => r.IsMemberAsync(1, "u1")).ReturnsAsync(false);
            teams.Setup(r => r.UserJoinedAnyTeamSinceAsync("u1", It.IsAny<DateTime>())).ReturnsAsync(false);
            teams.Setup(r => r.CountMembersJoinedSinceAsync(1, It.IsAny<DateTime>())).ReturnsAsync(1);

            var result = await CreateService(teams).JoinByCodeAsync(1, "ABC123", "u1");

            Assert.True(result.Success);
            teams.Verify(r => r.AddMemberAsync(1, "u1"), Times.Once);
        }
    }
}
