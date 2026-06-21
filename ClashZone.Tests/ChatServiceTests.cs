using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class ChatServiceTests
    {
        private static ChatService CreateService(Mock<ITournamentsRepository> tournaments, Mock<IUserRepository>? users = null)
            => new ChatService(tournaments.Object, (users ?? new Mock<IUserRepository>()).Object);

        [Fact]
        public async Task GetChatAsync_ReturnsNull_WhenTournamentMissing()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync((TournamentDomain?)null);

            Assert.Null(await CreateService(tournaments).GetChatAsync(1, "u1", false));
        }

        [Fact]
        public async Task GetChatAsync_LoadsTeamMessagesAndUserNames_WhenUserHasTeam()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetTournamentByIdAsync(1)).ReturnsAsync(new TournamentDomain { Id = 1 });
            tournaments.Setup(r => r.GetUserTeamAsync(1, "u1")).ReturnsAsync(new TeamDomain { Id = 5, CaptainId = "u1" });
            tournaments.Setup(r => r.GetAllChatMessagesAsync(1))
                .ReturnsAsync(new List<ChatMessageDomain> { new() { UserId = "u1", Message = "hi" } });
            tournaments.Setup(r => r.GetTeamChatMessagesAsync(1, 5))
                .ReturnsAsync(new List<ChatMessageDomain> { new() { UserId = "u2", Message = "team" } });
            tournaments.Setup(r => r.GetReportChatMessagesAsync(1, 5, false))
                .ReturnsAsync(new List<ChatMessageDomain>());

            var users = new Mock<IUserRepository>();
            users.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ClashUserDomain { Id = id, UserName = "name_" + id });

            var model = await CreateService(tournaments, users).GetChatAsync(1, "u1", false);

            Assert.NotNull(model);
            Assert.True(model!.HasTeam);
            Assert.Equal(5, model.UserTeamId);
            Assert.Single(model.TeamMessages);
            Assert.Equal("name_u1", model.UserNames["u1"]);
            Assert.Equal("name_u2", model.UserNames["u2"]);
        }

        [Fact]
        public async Task PostMessageAsync_SkipsEmptyMessage()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            await CreateService(tournaments).PostMessageAsync(1, "u1", "   ", false);
            tournaments.Verify(r => r.AddChatMessageAsync(It.IsAny<ChatMessageDomain>()), Times.Never);
        }

        [Fact]
        public async Task PostMessageAsync_ToTeam_WithoutTeam_DoesNotPost()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetUserTeamAsync(1, "u1")).ReturnsAsync((TeamDomain?)null);

            await CreateService(tournaments).PostMessageAsync(1, "u1", "hello", toTeam: true);

            tournaments.Verify(r => r.AddChatMessageAsync(It.IsAny<ChatMessageDomain>()), Times.Never);
        }

        [Fact]
        public async Task PostMessageAsync_General_AddsMessageWithoutTeam()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            ChatMessageDomain? saved = null;
            tournaments.Setup(r => r.AddChatMessageAsync(It.IsAny<ChatMessageDomain>()))
                .Callback<ChatMessageDomain>(m => saved = m).Returns(Task.CompletedTask);

            await CreateService(tournaments).PostMessageAsync(1, "u1", "hello", toTeam: false);

            Assert.NotNull(saved);
            Assert.Null(saved!.TeamId);
            Assert.False(saved.IsReport);
            Assert.Equal("hello", saved.Message);
        }

        [Fact]
        public async Task PostReportMessageAsync_Admin_WithoutTeamId_DoesNotPost()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            await CreateService(tournaments).PostReportMessageAsync(1, "admin", "report", teamId: null, isAdmin: true);
            tournaments.Verify(r => r.AddChatMessageAsync(It.IsAny<ChatMessageDomain>()), Times.Never);
        }

        [Fact]
        public async Task PostReportMessageAsync_Player_WithTeam_AddsReport()
        {
            var tournaments = new Mock<ITournamentsRepository>();
            tournaments.Setup(r => r.GetUserTeamAsync(1, "u1")).ReturnsAsync(new TeamDomain { Id = 9, CaptainId = "u1" });
            ChatMessageDomain? saved = null;
            tournaments.Setup(r => r.AddChatMessageAsync(It.IsAny<ChatMessageDomain>()))
                .Callback<ChatMessageDomain>(m => saved = m).Returns(Task.CompletedTask);

            await CreateService(tournaments).PostReportMessageAsync(1, "u1", "problem", teamId: null, isAdmin: false);

            Assert.NotNull(saved);
            Assert.True(saved!.IsReport);
            Assert.Equal(9, saved.TeamId);
        }
    }
}
