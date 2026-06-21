using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Domains.Riot;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Ports.Games;
using ClashZone.App.Services;
using ClashZone.App.Services.Interfaces;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class BracketServiceTests
    {
        private static BracketService CreateService(
            Mock<ITournamentsRepository> tournaments,
            Mock<IMatchesRepository>? matches = null,
            Mock<IPlayerStatsRepository>? stats = null,
            Mock<IRiotMatchService>? riot = null)
            => new BracketService(
                tournaments.Object,
                new Mock<IUserRepository>().Object,
                new Mock<IEmailService>().Object,
                new Mock<ICoinWalletService>().Object,
                (matches ?? new Mock<IMatchesRepository>()).Object,
                (stats ?? new Mock<IPlayerStatsRepository>()).Object,
                (riot ?? new Mock<IRiotMatchService>()).Object);

        [Fact]
        public async Task GetBracketAsync_ReturnsNull_WhenTournamentMissing()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync((TournamentDomain?)null);

            Assert.Null(await CreateService(tournaments).GetBracketAsync(1));
        }

        [Fact]
        public async Task GetBracketAsync_ReturnsEmptyRounds_WhenFewerThanTwoTeams()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Cs2 });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(new List<TeamDomain> { new() { Id = 1, Name = "Solo", CaptainId = "c1" } });

            var model = await CreateService(tournaments).GetBracketAsync(1);

            Assert.NotNull(model);
            Assert.Empty(model!.Rounds);
        }

        [Fact]
        public async Task SimulateMatchAsync_ReturnsNull_WhenTournamentMissing()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync((TournamentDomain?)null);

            Assert.Null(await CreateService(tournaments).SimulateMatchAsync(1, 1, 1));
        }

        [Fact]
        public async Task SimulateMatchAsync_ReturnsNull_WhenFewerThanTwoTeams()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Cs2 });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1))
                .ReturnsAsync(new List<TeamDomain> { new() { Id = 1, Name = "Solo", CaptainId = "c1" } });

            Assert.Null(await CreateService(tournaments).SimulateMatchAsync(1, 1, 1));
        }

        private static List<TeamDomain> FourTeams() => new()
        {
            new() { Id = 1, Name = "Alpha", CaptainId = "cap1" },
            new() { Id = 2, Name = "Bravo", CaptainId = "cap2" },
            new() { Id = 3, Name = "Charlie", CaptainId = "cap3" },
            new() { Id = 4, Name = "Delta", CaptainId = "cap4" },
        };

        [Fact]
        public async Task GetBracketWithResultsAsync_ReturnsNull_WhenTournamentMissing()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync((TournamentDomain?)null);

            Assert.Null(await CreateService(tournaments).GetBracketWithResultsAsync(1));
        }

        [Fact]
        public async Task GetBracketWithResultsAsync_GeneratesScoredBracket_WithoutPersisting()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Cs2 });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1)).ReturnsAsync(FourTeams());

            var matches = new Mock<IMatchesRepository>();
            var stats = new Mock<IPlayerStatsRepository>();

            var model = await CreateService(tournaments, matches, stats).GetBracketWithResultsAsync(1);

            Assert.NotNull(model);
            Assert.Equal(2, model!.Rounds.Count);
            Assert.Equal(2, model.Rounds[0].Count);
            Assert.Single(model.Rounds[1]);
            Assert.All(model.Rounds[0], m =>
            {
                Assert.False(string.IsNullOrEmpty(m.Team1Name));
                Assert.False(string.IsNullOrEmpty(m.Team2Name));
                Assert.True(m.Team1Score.HasValue);
                Assert.True(m.Team2Score.HasValue);
            });
            matches.Verify(r => r.AddMatchAsync(It.IsAny<MatchDomain>()), Times.Never);
            stats.Verify(r => r.AddPlayerMatchStatAsync(It.IsAny<PlayerMatchStatDomain>()), Times.Never);
        }

        [Fact]
        public async Task GetBracketWithStatsAsync_ReturnsNull_WhenTournamentMissing()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync((TournamentDomain?)null);

            Assert.Null(await CreateService(tournaments).GetBracketWithStatsAsync(1));
        }

        [Fact]
        public async Task GetBracketWithStatsAsync_PersistsMatchesAndPlayerStats()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Cs2 });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1)).ReturnsAsync(FourTeams());
            tournaments.Setup(r => r.GetTeamMemberIdsAsync(It.IsAny<int>())).ReturnsAsync(new List<string>());

            var matches = new Mock<IMatchesRepository>();
            matches.Setup(r => r.TournamentHasMatchesAsync(1)).ReturnsAsync(false);
            var addedMatches = new List<MatchDomain>();
            matches.Setup(r => r.AddMatchAsync(It.IsAny<MatchDomain>()))
                   .Callback<MatchDomain>(m => addedMatches.Add(m))
                   .Returns(Task.CompletedTask);

            var stats = new Mock<IPlayerStatsRepository>();
            stats.Setup(r => r.GetUserStatsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((UserStatDomain?)null);
            var addedStats = new List<PlayerMatchStatDomain>();
            stats.Setup(r => r.AddPlayerMatchStatAsync(It.IsAny<PlayerMatchStatDomain>()))
                 .Callback<PlayerMatchStatDomain>(s => addedStats.Add(s))
                 .Returns(Task.CompletedTask);

            var model = await CreateService(tournaments, matches, stats).GetBracketWithStatsAsync(1);

            Assert.NotNull(model);
            Assert.Equal(2, model!.Rounds.Count);
            Assert.Equal(3, addedMatches.Count);
            Assert.All(addedMatches, m => Assert.Equal(1, m.TournamentId));
            Assert.Equal(6, addedStats.Count);
            Assert.All(addedStats, s =>
            {
                Assert.Equal(GameCatalog.Cs2, s.Game);
                Assert.Null(s.Champion);
            });
            stats.Verify(r => r.AddUserStatsAsync(It.IsAny<UserStatDomain>()), Times.Exactly(6));
            stats.Verify(r => r.UpdateUserStats(It.IsAny<UserStatDomain>()), Times.Never);
        }

        [Fact]
        public async Task GetBracketWithStatsAsync_AssignsChampions_ForLeagueOfLegends()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Lol });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1)).ReturnsAsync(FourTeams());
            tournaments.Setup(r => r.GetTeamMemberIdsAsync(It.IsAny<int>())).ReturnsAsync(new List<string>());

            var matches = new Mock<IMatchesRepository>();
            matches.Setup(r => r.TournamentHasMatchesAsync(1)).ReturnsAsync(false);

            var stats = new Mock<IPlayerStatsRepository>();
            stats.Setup(r => r.GetUserStatsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((UserStatDomain?)null);
            var addedStats = new List<PlayerMatchStatDomain>();
            stats.Setup(r => r.AddPlayerMatchStatAsync(It.IsAny<PlayerMatchStatDomain>()))
                 .Callback<PlayerMatchStatDomain>(s => addedStats.Add(s))
                 .Returns(Task.CompletedTask);

            var model = await CreateService(tournaments, matches, stats).GetBracketWithStatsAsync(1);

            Assert.NotNull(model);
            Assert.NotEmpty(addedStats);
            Assert.All(addedStats, s =>
            {
                Assert.Equal(GameCatalog.Lol, s.Game);
                Assert.False(string.IsNullOrEmpty(s.Champion));
                Assert.Contains(s.Champion!, GameCatalog.Champions);
            });
        }

        private static Mock<ITournamentsRepository> TwoLolTeams()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1, GameTitle = GameCatalog.Lol });
            tournaments.Setup(r => r.GetTeamsForTournamentAsync(1)).ReturnsAsync(new List<TeamDomain>
            {
                new() { Id = 1, Name = "Alpha", CaptainId = "cap1" },
                new() { Id = 2, Name = "Bravo", CaptainId = "cap2" },
            });
            tournaments.Setup(r => r.GetTeamMemberIdsAsync(It.IsAny<int>())).ReturnsAsync(new List<string>());
            return tournaments;
        }

        [Fact]
        public async Task SimulateMatchAsync_ForLol_PersistsRealRiotData()
        {
            var tournaments = TwoLolTeams();

            var matches = new Mock<IMatchesRepository>();
            matches.Setup(r => r.GetMatchesByTournamentAsync(1)).ReturnsAsync(new List<MatchDomain>());
            matches.Setup(r => r.IsTournamentAlreadyAwardedAsync(It.IsAny<int>(), It.IsAny<List<List<MatchInfoDomain>>>())).ReturnsAsync(true);
            var addedMatches = new List<MatchDomain>();
            matches.Setup(r => r.AddMatchAsync(It.IsAny<MatchDomain>()))
                   .Callback<MatchDomain>(m => addedMatches.Add(m))
                   .Returns(Task.CompletedTask);

            var stats = new Mock<IPlayerStatsRepository>();
            var addedStats = new List<PlayerMatchStatDomain>();
            stats.Setup(r => r.AddPlayerMatchStatAsync(It.IsAny<PlayerMatchStatDomain>()))
                 .Callback<PlayerMatchStatDomain>(s => addedStats.Add(s))
                 .Returns(Task.CompletedTask);

            var riot = new Mock<IRiotMatchService>();
            riot.Setup(r => r.ResolveMatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RiotMatchOutcome
                {
                    Team1Score = 1,
                    Team2Score = 0,
                    Players = new List<RiotMatchPlayerStat>
                    {
                        new() { UserId = "cap1", Kills = 7, Deaths = 2, Assists = 10, Champion = "Ahri" },
                        new() { UserId = "cap2", Kills = 3, Deaths = 8, Assists = 5, Champion = "Zed" },
                    }
                });

            var model = await CreateService(tournaments, matches, stats, riot).SimulateMatchAsync(1, 1, 1);

            Assert.NotNull(model);
            var persisted = Assert.Single(addedMatches);
            Assert.Equal(1, persisted.Team1Score);
            Assert.Equal(0, persisted.Team2Score);
            Assert.Equal(2, addedStats.Count);
            Assert.Contains(addedStats, s => s.UserId == "cap1" && s.Kills == 7 && s.Champion == "Ahri");
            Assert.Contains(addedStats, s => s.UserId == "cap2" && s.Deaths == 8 && s.Champion == "Zed");
            Assert.All(addedStats, s => Assert.Equal(GameCatalog.Lol, s.Game));
            riot.Verify(r => r.ResolveMatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SimulateMatchAsync_ForLol_ReturnsNull_AndPersistsNothing_WhenNoRiotData()
        {
            var tournaments = TwoLolTeams();

            var matches = new Mock<IMatchesRepository>();
            matches.Setup(r => r.GetMatchesByTournamentAsync(1)).ReturnsAsync(new List<MatchDomain>());

            var stats = new Mock<IPlayerStatsRepository>();

            var riot = new Mock<IRiotMatchService>();
            riot.Setup(r => r.ResolveMatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RiotMatchOutcome?)null);

            var model = await CreateService(tournaments, matches, stats, riot).SimulateMatchAsync(1, 1, 1);

            Assert.Null(model);
            matches.Verify(r => r.AddMatchAsync(It.IsAny<MatchDomain>()), Times.Never);
            stats.Verify(r => r.AddPlayerMatchStatAsync(It.IsAny<PlayerMatchStatDomain>()), Times.Never);
        }
    }
}
