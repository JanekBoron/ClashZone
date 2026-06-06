using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using ClashZone.App.Services.Interfaces;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="LeagueService"/> — rejestracja drużyn (wymóg pakietu,
    /// rozmiar rosteru) oraz wyliczanie tabeli (standings).
    /// </summary>
    public class LeagueServiceTests
    {
        private readonly Mock<ILeagueRepository> _league = new();
        private readonly Mock<ILeagueTeamsRepository> _teams = new();
        private readonly Mock<ISubscriptionRepository> _subs = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<IPlayerStatsRepository> _stats = new();
        private readonly Mock<ICoinWalletService> _wallet = new();

        private LeagueService Service => new(
            _league.Object, _teams.Object, _subs.Object, _users.Object, _stats.Object, _wallet.Object);

        private static SeasonDomain OpenSeason() => new()
        {
            Id = 1,
            Game = "Counter Strike 2",
            RosterSize = 5,
            Status = (int)SeasonStatus.Registration,
            RegistrationOpenDate = DateTime.UtcNow.AddDays(-1),
            RegistrationCloseDate = DateTime.UtcNow.AddDays(1)
        };

        private void WithLeaguePackage(string userId)
            => _subs.Setup(s => s.GetActiveSubscriptionAsync(userId))
                    .ReturnsAsync(new UserSubscriptionDomain { Plan = new SubscriptionPlanDomain { IsLeagueAccess = true } });

        [Fact]
        public async Task RegisterTeamAsync_RequiresLeaguePackage_WhenNoSubscription()
        {
            _league.Setup(r => r.GetSeasonByIdAsync(1)).ReturnsAsync(OpenSeason());
            _subs.Setup(s => s.GetActiveSubscriptionAsync("u1")).ReturnsAsync((UserSubscriptionDomain?)null);

            var result = await Service.RegisterTeamAsync(1, 10, "u1");

            Assert.True(result.RequiresLeaguePackage);
            _league.Verify(r => r.AddRegistrationAsync(It.IsAny<LeagueRegistrationDomain>()), Times.Never);
        }

        [Fact]
        public async Task RegisterTeamAsync_RosterTooSmall_WhenBelowRequired()
        {
            _league.Setup(r => r.GetSeasonByIdAsync(1)).ReturnsAsync(OpenSeason());
            WithLeaguePackage("u1");
            _teams.Setup(t => t.GetByIdAsync(10)).ReturnsAsync(new LeagueTeamDomain { Id = 10, CaptainId = "u1" });
            _league.Setup(r => r.GetRegistrationAsync(1, 10)).ReturnsAsync((LeagueRegistrationDomain?)null);
            _teams.Setup(t => t.GetMemberIdsAsync(10)).ReturnsAsync(new List<string> { "u1", "u2" }); // 2 < 5

            var result = await Service.RegisterTeamAsync(1, 10, "u1");

            Assert.True(result.RosterTooSmall);
            _league.Verify(r => r.AddRegistrationAsync(It.IsAny<LeagueRegistrationDomain>()), Times.Never);
        }

        [Fact]
        public async Task RegisterTeamAsync_Succeeds_WithPackageAndFullRoster()
        {
            _league.Setup(r => r.GetSeasonByIdAsync(1)).ReturnsAsync(OpenSeason());
            WithLeaguePackage("u1");
            _teams.Setup(t => t.GetByIdAsync(10)).ReturnsAsync(new LeagueTeamDomain { Id = 10, CaptainId = "u1" });
            _league.Setup(r => r.GetRegistrationAsync(1, 10)).ReturnsAsync((LeagueRegistrationDomain?)null);
            _teams.Setup(t => t.GetMemberIdsAsync(10))
                  .ReturnsAsync(new List<string> { "u1", "u2", "u3", "u4", "u5" });

            var result = await Service.RegisterTeamAsync(1, 10, "u1");

            Assert.True(result.Success);
            _league.Verify(r => r.AddRegistrationAsync(It.Is<LeagueRegistrationDomain>(reg =>
                reg.SeasonId == 1 && reg.LeagueTeamId == 10 && reg.FeePaid)), Times.Once);
        }

        [Fact]
        public async Task GetStandingsAsync_ComputesWinsLossesAndOrdersByRatio()
        {
            _league.Setup(r => r.GetRegistrationsForSeasonAsync(1, false)).ReturnsAsync(new List<LeagueRegistrationDomain>
            {
                new() { Id = 100, LeagueTeamId = 1, LeagueTeam = new LeagueTeamDomain { Id = 1, Name = "Alpha" } },
                new() { Id = 200, LeagueTeamId = 2, LeagueTeam = new LeagueTeamDomain { Id = 2, Name = "Bravo" } }
            });
            _league.Setup(r => r.GetMatchesForSeasonAsync(1)).ReturnsAsync(new List<LeagueMatchDomain>
            {
                // Alpha (100) beats Bravo (200)
                new() { Id = 1, HomeRegistrationId = 100, AwayRegistrationId = 200, HomeScore = 16, AwayScore = 7, IsPlayed = true },
                // Alpha (100) beats Bravo (200) again
                new() { Id = 2, HomeRegistrationId = 200, AwayRegistrationId = 100, HomeScore = 5, AwayScore = 16, IsPlayed = true },
                // unplayed should be ignored
                new() { Id = 3, HomeRegistrationId = 100, AwayRegistrationId = 200, IsPlayed = false }
            });

            var standings = await Service.GetStandingsAsync(1);

            Assert.Equal(2, standings.Count);
            Assert.Equal("Alpha", standings[0].TeamName);
            Assert.Equal(2, standings[0].Wins);
            Assert.Equal(0, standings[0].Losses);
            Assert.Equal(2, standings[0].Played);
            Assert.Equal("Bravo", standings[1].TeamName);
            Assert.Equal(0, standings[1].Wins);
            Assert.Equal(2, standings[1].Losses);
        }
    }
}
