using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
// Alias the domain Match type to avoid clashing with Moq.Match.
using DataMatch = DataAccess.Models.Match;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MatchesService"/>.  These tests cover both
    /// the list and details retrieval methods, ensuring that friendly names
    /// and avatar URLs are computed correctly and that statistics are
    /// organized into the appropriate lists.  By mocking the underlying
    /// repository and user manager, we can focus on the translation logic
    /// implemented in the service.
    /// </summary>
    public class MatchesServiceTests
    {
        /// <summary>
        /// Creates a minimal <see cref="UserManager{TUser}"/> instance backed by a mocked
        /// <see cref="IUserStore{TUser}"/>.  The default implementation doesn't
        /// require password validation or other infrastructure that isn't needed
        /// for unit tests.
        /// </summary>
        private static UserManager<ClashUser> CreateUserManager()
        {
            var store = new Mock<IUserStore<ClashUser>>();
            var userValidators = new List<IUserValidator<ClashUser>>();
            var pwdValidators = new List<IPasswordValidator<ClashUser>>();
            return new UserManager<ClashUser>(store.Object,
                null, null, userValidators, pwdValidators, null, null, null, null);
        }

        [Fact]
        public async Task GetMatchesForTournamentAsync_ReturnsItems_WithTeamNamesAndProfiles()
        {
            // Arrange
            int tournamentId = 1;
            // Two matches: one with both teams, one with a missing second team
            var match1 = new DataMatch { Id = 1, Team1Id = 1, Team2Id = 2 };
            var match2 = new DataMatch { Id = 2, Team1Id = 3, Team2Id = null };
            var matches = new List<DataMatch> { match1, match2 };

            var repoMock = new Mock<IMatchesRepository>();
            repoMock.Setup(r => r.GetMatchesByTournamentAsync(tournamentId))
                .ReturnsAsync(matches);
            // Teams for the first match have captains with profile pictures
            var team1 = new Team { Id = 1, Name = "Alpha", CaptainId = "captain1" };
            var team2 = new Team { Id = 2, Name = "Bravo", CaptainId = "captain2" };
            var team3 = new Team { Id = 3, Name = "Charlie", CaptainId = "captain3" };
            repoMock.Setup(r => r.GetTeamByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    return id switch
                    {
                        1 => team1,
                        2 => team2,
                        3 => team3,
                        _ => null
                    };
                });

            // Captains: some have a custom profile picture, others do not
            var users = new Dictionary<string, ClashUser>
            {
                { "captain1", new ClashUser { Id = "captain1", ProfilePicturePath = "/avatars/captain1.png" } },
                { "captain2", new ClashUser { Id = "captain2", ProfilePicturePath = "/avatars/captain2.png" } },
                { "captain3", new ClashUser { Id = "captain3", ProfilePicturePath = null } }
            };
            var userManagerMock = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => users.TryGetValue(id, out var user) ? user : null);

            var service = new MatchesService(repoMock.Object, Mock.Of<ITournamentsRepository>(), userManagerMock.Object);

            // Act
            var result = await service.GetMatchesForTournamentAsync(tournamentId);

            // Assert
            Assert.Equal(2, result.Count);
            var item1 = result[0];
            Assert.Equal(match1, item1.Match);
            Assert.Equal("Alpha", item1.Team1Name);
            Assert.Equal("Bravo", item1.Team2Name);
            Assert.Equal("/avatars/captain1.png", item1.Team1ProfileUrl);
            Assert.Equal("/avatars/captain2.png", item1.Team2ProfileUrl);

            var item2 = result[1];
            Assert.Equal(match2, item2.Match);
            // Team3 exists, team2 is missing so name should be BYE
            Assert.Equal("Charlie", item2.Team1Name);
            Assert.Equal("BYE", item2.Team2Name);
            // captain3 has no profile picture so default should be used
            Assert.Equal("/images/default-profile.png", item2.Team1ProfileUrl);
            Assert.Equal("/images/default-profile.png", item2.Team2ProfileUrl);
        }

        [Fact]
        public async Task GetMatchesForTournamentAsync_AssignsBYEAndDefaultProfile_WhenNoTeams()
        {
            // Arrange
            int tournamentId = 1;
            var match = new DataMatch { Id = 1, Team1Id = null, Team2Id = null };
            var repoMock = new Mock<IMatchesRepository>();
            repoMock.Setup(r => r.GetMatchesByTournamentAsync(tournamentId))
                .ReturnsAsync(new List<DataMatch> { match });
            var userManager = CreateUserManager();
            // No teams to resolve; GetTeamByIdAsync shouldn't be called but return null anyway
            repoMock.Setup(r => r.GetTeamByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Team?)null);
            var service = new MatchesService(repoMock.Object, Mock.Of<ITournamentsRepository>(), userManager);

            // Act
            var items = await service.GetMatchesForTournamentAsync(tournamentId);

            // Assert
            var item = Assert.Single(items);
            Assert.Equal("BYE", item.Team1Name);
            Assert.Equal("BYE", item.Team2Name);
            Assert.Equal("/images/default-profile.png", item.Team1ProfileUrl);
            Assert.Equal("/images/default-profile.png", item.Team2ProfileUrl);
        }

        [Fact]
        public async Task GetMatchDetailsAsync_ReturnsNull_WhenMatchNotFound()
        {
            // Arrange
            var repoMock = new Mock<IMatchesRepository>();
            repoMock.Setup(r => r.GetMatchByIdAsync(1, 1))
                .ReturnsAsync((DataMatch?)null);
            var service = new MatchesService(repoMock.Object, Mock.Of<ITournamentsRepository>(), CreateUserManager());

            // Act
            var result = await service.GetMatchDetailsAsync(1, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetMatchDetailsAsync_ReturnsModel_WithStatsAndTeams()
        {
            // Arrange
            int tournamentId = 1;
            int matchId = 1;
            var match = new DataMatch { Id = matchId, Team1Id = 1, Team2Id = 2 };
            var repoMock = new Mock<IMatchesRepository>();
            repoMock.Setup(r => r.GetMatchByIdAsync(matchId, tournamentId))
                .ReturnsAsync(match);
            // Teams and captains
            var team1 = new Team { Id = 1, Name = "Alpha", CaptainId = "captain1" };
            var team2 = new Team { Id = 2, Name = "Bravo", CaptainId = "captain2" };
            repoMock.Setup(r => r.GetTeamByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => id == 1 ? team1 : id == 2 ? team2 : null);
            var users = new Dictionary<string, ClashUser>
            {
                { "captain1", new ClashUser { Id = "captain1", ProfilePicturePath = "/p/c1.png" } },
                { "captain2", new ClashUser { Id = "captain2", ProfilePicturePath = "/p/c2.png" } },
                { "player1", new ClashUser { Id = "player1", UserName = "Player1" } },
                { "player2", new ClashUser { Id = "player2", UserName = "Player2" } }
            };
            var userManagerMock = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => users.TryGetValue(id, out var u) ? u : null);
            // Player stats: player1 belongs to team1, player2 belongs to team2 (by default)
            var stats = new List<PlayerMatchStat> {
                new PlayerMatchStat { UserId = "player1", Kills = 5, Deaths = 2, Assists = 3 },
                new PlayerMatchStat { UserId = "player2", Kills = 2, Deaths = 5, Assists = 1 }
            };
            repoMock.Setup(r => r.GetPlayerStatsForMatchAsync(match.Id))
                .ReturnsAsync(stats);
            // Tournaments repository returns membership for team1 only
            var tournamentsRepoMock = new Mock<ITournamentsRepository>();
            tournamentsRepoMock.Setup(tr => tr.GetTeamMemberIdsAsync(team1.Id))
                .ReturnsAsync(new List<string> { "player1" });

            var service = new MatchesService(repoMock.Object, tournamentsRepoMock.Object, userManagerMock.Object);

            // Act
            var model = await service.GetMatchDetailsAsync(tournamentId, matchId);

            // Assert
            Assert.NotNull(model);
            Assert.Equal(match, model!.Match);
            Assert.Equal("Alpha", model.Team1Name);
            Assert.Equal("Bravo", model.Team2Name);
            Assert.Equal("/p/c1.png", model.Team1ProfileUrl);
            Assert.Equal("/p/c2.png", model.Team2ProfileUrl);
            // Stats lists
            Assert.Single(model.Team1Stats);
            Assert.Single(model.Team2Stats);
            Assert.Equal("player1", model.Team1Stats.First().Player.Id);
            Assert.Equal(5, model.Team1Stats.First().Kills);
            Assert.Equal("player2", model.Team2Stats.First().Player.Id);
        }

        [Fact]
        public async Task GetMatchDetailsAsync_AssignsBYEAndDefaults_WhenTeamsNull()
        {
            // Arrange
            int tournamentId = 1;
            int matchId = 2;
            var match = new DataMatch { Id = matchId, Team1Id = null, Team2Id = null };
            var repoMock = new Mock<IMatchesRepository>();
            repoMock.Setup(r => r.GetMatchByIdAsync(matchId, tournamentId))
                .ReturnsAsync(match);
            repoMock.Setup(r => r.GetTeamByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Team?)null);
            // Stats: two players
            var stats = new List<PlayerMatchStat>
            {
                new PlayerMatchStat { UserId = "player1", Kills = 1, Deaths = 1, Assists = 0 },
                new PlayerMatchStat { UserId = "player2", Kills = 0, Deaths = 1, Assists = 2 }
            };
            repoMock.Setup(r => r.GetPlayerStatsForMatchAsync(match.Id))
                .ReturnsAsync(stats);
            // Tournaments repository should never be called for member IDs since no teams exist
            var tournamentsRepoMock = new Mock<ITournamentsRepository>();
            // user manager returns users for players
            var userManagerMock = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ClashUser { Id = id, UserName = id });
            var service = new MatchesService(repoMock.Object, tournamentsRepoMock.Object, userManagerMock.Object);

            // Act
            var model = await service.GetMatchDetailsAsync(tournamentId, matchId);

            // Assert
            Assert.NotNull(model);
            Assert.Equal("BYE", model!.Team1Name);
            Assert.Equal("BYE", model.Team2Name);
            Assert.Equal("/images/default-profile.png", model.Team1ProfileUrl);
            Assert.Equal("/images/default-profile.png", model.Team2ProfileUrl);
            // With no team membership, all players should be assigned to team2 by default
            Assert.Empty(model.Team1Stats);
            Assert.Equal(2, model.Team2Stats.Count);
        }
    }
}