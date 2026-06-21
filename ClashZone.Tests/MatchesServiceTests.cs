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
    public class MatchesServiceTests
    {
        private static MatchesService CreateService(
            Mock<IMatchesRepository> matches,
            Mock<ITournamentsRepository> tournaments,
            Mock<IUserRepository> users)
            => new MatchesService(matches.Object, tournaments.Object, users.Object);

        [Fact]
        public async Task GetMatchesForTournamentAsync_ResolvesNamesProfilesAndGame()
        {
            int tournamentId = 1;
            var match1 = new MatchDomain { Id = 1, TournamentId = tournamentId, Team1Id = 1, Team2Id = 2 };
            var match2 = new MatchDomain { Id = 2, TournamentId = tournamentId, Team1Id = 3, Team2Id = null };

            var matches = new Mock<IMatchesRepository>();
            matches.Setup(r => r.GetMatchesByTournamentAsync(tournamentId))
                .ReturnsAsync(new List<MatchDomain> { match1, match2 });
            matches.Setup(r => r.GetTeamByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => id switch
                {
                    1 => new TeamDomain { Id = 1, Name = "Alpha", CaptainId = "c1" },
                    2 => new TeamDomain { Id = 2, Name = "Bravo", CaptainId = "c2" },
                    3 => new TeamDomain { Id = 3, Name = "Charlie", CaptainId = "c3" },
                    _ => null
                });

            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(tournamentId))
                .ReturnsAsync(new TournamentDomain { Id = tournamentId, GameTitle = GameCatalog.Lol });

            var users = new Mock<IUserRepository>();
            users.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => id switch
                {
                    "c1" => new ClashUserDomain { Id = "c1", ProfilePicturePath = "/avatars/c1.png" },
                    "c2" => new ClashUserDomain { Id = "c2", ProfilePicturePath = "/avatars/c2.png" },
                    _ => new ClashUserDomain { Id = id, ProfilePicturePath = null }
                });

            var result = await CreateService(matches, tournaments, users).GetMatchesForTournamentAsync(tournamentId);

            Assert.Equal(2, result.Count);
            Assert.Equal("Alpha", result[0].Team1Name);
            Assert.Equal("Bravo", result[0].Team2Name);
            Assert.Equal("/avatars/c1.png", result[0].Team1ProfileUrl);
            Assert.Equal(GameCatalog.Lol, result[0].GameTitle);
            Assert.Equal("Charlie", result[1].Team1Name);
            Assert.Equal("BYE", result[1].Team2Name);
            Assert.Equal("/images/default-profile.png", result[1].Team1ProfileUrl);
        }

        [Fact]
        public async Task GetMatchDetailsAsync_ReturnsNull_WhenMatchMissing()
        {
            var matches = new Mock<IMatchesRepository>();
            matches.Setup(r => r.GetMatchByIdAsync(1, 1)).ReturnsAsync((MatchDomain?)null);

            var service = CreateService(matches, new Mock<ITournamentsRepository>(), new Mock<IUserRepository>());

            Assert.Null(await service.GetMatchDetailsAsync(1, 1));
        }

        [Fact]
        public async Task GetMatchDetailsAsync_SplitsStatsByMembership_AndMapsChampionAndGame()
        {
            int tournamentId = 1, matchId = 5;
            var match = new MatchDomain { Id = matchId, TournamentId = tournamentId, Team1Id = 1, Team2Id = 2, Team1Score = 2, Team2Score = 1 };

            var matches = new Mock<IMatchesRepository>();
            matches.Setup(r => r.GetMatchByIdAsync(matchId, tournamentId)).ReturnsAsync(match);
            matches.Setup(r => r.GetTeamByIdAsync(1)).ReturnsAsync(new TeamDomain { Id = 1, Name = "Alpha", CaptainId = "c1" });
            matches.Setup(r => r.GetTeamByIdAsync(2)).ReturnsAsync(new TeamDomain { Id = 2, Name = "Bravo", CaptainId = "c2" });
            matches.Setup(r => r.GetPlayerStatsForMatchAsync(matchId)).ReturnsAsync(new List<PlayerMatchStatDomain>
            {
                new PlayerMatchStatDomain { UserId = "p1", Kills = 5, Deaths = 2, Assists = 3, Champion = "Ahri", Game = GameCatalog.Lol },
                new PlayerMatchStatDomain { UserId = "p2", Kills = 1, Deaths = 4, Assists = 6, Champion = "Garen", Game = GameCatalog.Lol }
            });

            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(tournamentId))
                .ReturnsAsync(new TournamentDomain { Id = tournamentId, GameTitle = GameCatalog.Lol });
            tournaments.Setup(r => r.GetTeamMemberIdsAsync(1)).ReturnsAsync(new List<string> { "p1" });

            var users = new Mock<IUserRepository>();
            users.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ClashUserDomain { Id = id, UserName = id });

            var model = await CreateService(matches, tournaments, users).GetMatchDetailsAsync(tournamentId, matchId);

            Assert.NotNull(model);
            Assert.Equal(GameCatalog.Lol, model!.GameTitle);
            var p1 = Assert.Single(model.Team1Stats);
            Assert.Equal("p1", p1.Player.Id);
            Assert.Equal("Ahri", p1.Champion);
            var p2 = Assert.Single(model.Team2Stats);
            Assert.Equal("Garen", p2.Champion);
        }
    }
}
