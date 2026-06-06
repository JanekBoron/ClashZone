using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Domains.Riot;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Ports.Games;
using ClashZone.App.Services.Games.Riot;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="RiotMatchService"/> — mapowania użytkowników ClashZone na
    /// uczestników prawdziwego meczu LoL oraz budowania wyniku z danych Riota.
    /// </summary>
    public class RiotMatchServiceTests
    {
        private static Mock<IUserRepository> UsersWithPuuids(params (string UserId, string? Puuid)[] users)
        {
            var repo = new Mock<IUserRepository>();
            foreach (var (userId, puuid) in users)
            {
                repo.Setup(r => r.FindByIdAsync(userId))
                    .ReturnsAsync(new ClashUserDomain { Id = userId, RiotPuuid = puuid });
            }
            return repo;
        }

        [Fact]
        public async Task ResolveMatchAsync_ReturnsNull_WhenApiNotConfigured()
        {
            var api = new Mock<IRiotApiClient>();
            api.SetupGet(a => a.IsConfigured).Returns(false);

            var service = new RiotMatchService(api.Object, new Mock<IUserRepository>().Object, new RiotOptions());

            Assert.Null(await service.ResolveMatchAsync(new[] { "u1" }, new[] { "u2" }));
        }

        [Fact]
        public async Task ResolveMatchAsync_ReturnsNull_WhenNoLinkedAccounts()
        {
            var api = new Mock<IRiotApiClient>();
            api.SetupGet(a => a.IsConfigured).Returns(true);
            var users = UsersWithPuuids(("u1", null), ("u2", null));

            var service = new RiotMatchService(api.Object, users.Object, new RiotOptions());

            Assert.Null(await service.ResolveMatchAsync(new[] { "u1" }, new[] { "u2" }));
            api.Verify(a => a.GetRecentMatchIdsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ResolveMatchAsync_BuildsOutcome_FromOpposingTeams()
        {
            var api = new Mock<IRiotApiClient>();
            api.SetupGet(a => a.IsConfigured).Returns(true);
            api.Setup(a => a.GetRecentMatchIdsAsync("p1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "EUW1_1" });
            api.Setup(a => a.GetMatchAsync("EUW1_1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RiotMatch
                {
                    MatchId = "EUW1_1",
                    Participants = new List<RiotMatchParticipant>
                    {
                        new() { Puuid = "p1", TeamId = 100, Win = true,  Kills = 7, Deaths = 2, Assists = 10, ChampionName = "Ahri" },
                        new() { Puuid = "p2", TeamId = 200, Win = false, Kills = 3, Deaths = 8, Assists = 5,  ChampionName = "Zed" },
                    }
                });
            var users = UsersWithPuuids(("u1", "p1"), ("u2", "p2"));

            var service = new RiotMatchService(api.Object, users.Object, new RiotOptions());

            var outcome = await service.ResolveMatchAsync(new[] { "u1" }, new[] { "u2" });

            Assert.NotNull(outcome);
            Assert.Equal("EUW1_1", outcome!.SourceMatchId);
            // Drużyna 1 wygrała grę => 1-0.
            Assert.Equal(1, outcome.Team1Score);
            Assert.Equal(0, outcome.Team2Score);
            Assert.Equal(2, outcome.Players.Count);
            Assert.Contains(outcome.Players, p => p.UserId == "u1" && p.Kills == 7 && p.Champion == "Ahri");
            Assert.Contains(outcome.Players, p => p.UserId == "u2" && p.Deaths == 8 && p.Champion == "Zed");
        }

        [Fact]
        public async Task ResolveMatchAsync_DevMode_BuildsOutcome_FromSeedLatestMatch()
        {
            // Tryb deweloperski: tylko jedno konto (admin = u1) jest powiązane;
            // u2 nie ma konta Riot, a mimo to budujemy wynik z najnowszego meczu admina.
            var api = new Mock<IRiotApiClient>();
            api.SetupGet(a => a.IsConfigured).Returns(true);
            api.Setup(a => a.GetRecentMatchIdsAsync("p1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "EUW1_42" });
            api.Setup(a => a.GetMatchAsync("EUW1_42", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RiotMatch
                {
                    MatchId = "EUW1_42",
                    Participants = new List<RiotMatchParticipant>
                    {
                        // Strona admina (przegrywa) -> drużyna 1 ClashZone.
                        new() { Puuid = "p1", TeamId = 200, Win = false, Kills = 1, Deaths = 9, Assists = 2, ChampionName = "Yasuo" },
                        // Strona przeciwna -> drużyna 2 ClashZone.
                        new() { Puuid = "stranger", TeamId = 100, Win = true, Kills = 12, Deaths = 1, Assists = 4, ChampionName = "Garen" },
                    }
                });
            var users = UsersWithPuuids(("u1", "p1"), ("u2", null));
            var options = new RiotOptions { ApiKey = "x", DevMode = true };

            var service = new RiotMatchService(api.Object, users.Object, options);

            var outcome = await service.ResolveMatchAsync(new[] { "u1" }, new[] { "u2" });

            Assert.NotNull(outcome);
            Assert.Equal("EUW1_42", outcome!.SourceMatchId);
            // Strona admina przegrała => drużyna 1 = 0, drużyna 2 = 1.
            Assert.Equal(0, outcome.Team1Score);
            Assert.Equal(1, outcome.Team2Score);
            // Staty zmapowane po kolei: u1 <- uczestnik strony admina, u2 <- strona przeciwna.
            Assert.Contains(outcome.Players, p => p.UserId == "u1" && p.Deaths == 9 && p.Champion == "Yasuo");
            Assert.Contains(outcome.Players, p => p.UserId == "u2" && p.Kills == 12 && p.Champion == "Garen");
        }

        [Fact]
        public async Task ResolveMatchAsync_ForceDevMode_BuildsOutcome_EvenWhenDevModeOff()
        {
            // Admin odpala symulację (forceDevMode), więc ścieżka dev działa mimo DevMode=false.
            var api = new Mock<IRiotApiClient>();
            api.SetupGet(a => a.IsConfigured).Returns(true);
            api.Setup(a => a.GetRecentMatchIdsAsync("p1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "EUW1_77" });
            api.Setup(a => a.GetMatchAsync("EUW1_77", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RiotMatch
                {
                    MatchId = "EUW1_77",
                    Participants = new List<RiotMatchParticipant>
                    {
                        new() { Puuid = "p1", TeamId = 100, Win = true, Kills = 8, Deaths = 3, Assists = 6, ChampionName = "Lux" },
                        new() { Puuid = "stranger", TeamId = 200, Win = false, Kills = 2, Deaths = 7, Assists = 1, ChampionName = "Teemo" },
                    }
                });
            var users = UsersWithPuuids(("u1", "p1"), ("u2", null));
            // DevMode wyłączony – wymuszamy ścieżkę dev tylko parametrem forceDevMode.
            var options = new RiotOptions { ApiKey = "x", DevMode = false };

            var service = new RiotMatchService(api.Object, users.Object, options);

            var outcome = await service.ResolveMatchAsync(new[] { "u1" }, new[] { "u2" }, forceDevMode: true);

            Assert.NotNull(outcome);
            Assert.Equal("EUW1_77", outcome!.SourceMatchId);
            Assert.Equal(1, outcome.Team1Score);
            Assert.Equal(0, outcome.Team2Score);
            Assert.Contains(outcome.Players, p => p.UserId == "u1" && p.Kills == 8 && p.Champion == "Lux");
            Assert.Contains(outcome.Players, p => p.UserId == "u2" && p.Deaths == 7 && p.Champion == "Teemo");
        }

        [Fact]
        public async Task ResolveMatchAsync_ReturnsNull_WhenTeamsOnSameSide()
        {
            var api = new Mock<IRiotApiClient>();
            api.SetupGet(a => a.IsConfigured).Returns(true);
            api.Setup(a => a.GetRecentMatchIdsAsync("p1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { "EUW1_1" });
            // Obaj gracze po tej samej stronie => to nie był mecz tych dwóch drużyn.
            api.Setup(a => a.GetMatchAsync("EUW1_1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RiotMatch
                {
                    MatchId = "EUW1_1",
                    Participants = new List<RiotMatchParticipant>
                    {
                        new() { Puuid = "p1", TeamId = 100, Win = true, ChampionName = "Ahri" },
                        new() { Puuid = "p2", TeamId = 100, Win = true, ChampionName = "Zed" },
                    }
                });
            var users = UsersWithPuuids(("u1", "p1"), ("u2", "p2"));

            var service = new RiotMatchService(api.Object, users.Object, new RiotOptions());

            Assert.Null(await service.ResolveMatchAsync(new[] { "u1" }, new[] { "u2" }));
        }
    }
}
