using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Unit tests for <see cref="ChatService"/>.  These tests verify that chat
    /// retrieval populates the view model correctly and that posting
    /// messages respects access control rules.  All interactions with
    /// persistent storage are mocked via <see cref="ITournamentsRepository"/>.
    /// </summary>
    public class ChatServiceTests
    {
        private static UserManager<ClashUser> CreateUserManager()
        {
            var store = new Mock<IUserStore<ClashUser>>();
            return new UserManager<ClashUser>(store.Object,
                null, null, new List<IUserValidator<ClashUser>>(), new List<IPasswordValidator<ClashUser>>(), null, null, null, null);
        }

        [Fact]
        public async Task GetChatAsync_ReturnsNull_WhenTournamentNotFound()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync((Tournament?)null);
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            var result = await service.GetChatAsync(1, null, false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetChatAsync_ReturnsViewModel_WithMessagesAndUserNames()
        {
            // Arrange
            int tournamentId = 2;
            var tournament = new Tournament { Id = tournamentId };
            var userTeam = new Team { Id = 5 };
            var allMessages = new List<ChatMessage>
            {
                new ChatMessage { Id = 1, TournamentId = tournamentId, UserId = "u1", Message = "Hello", SentAt = DateTime.UtcNow, IsReport = false }
            };
            var teamMessages = new List<ChatMessage>
            {
                new ChatMessage { Id = 2, TournamentId = tournamentId, TeamId = 5, UserId = "u2", Message = "Team", SentAt = DateTime.UtcNow, IsReport = false }
            };
            var reportMessages = new List<ChatMessage>
            {
                new ChatMessage { Id = 3, TournamentId = tournamentId, TeamId = 5, UserId = "u3", Message = "Report", SentAt = DateTime.UtcNow, IsReport = true }
            };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(tournamentId))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetUserTeamAsync(tournamentId, "user"))
                .ReturnsAsync(userTeam);
            repoMock.Setup(r => r.GetAllChatMessagesAsync(tournamentId))
                .ReturnsAsync(allMessages);
            repoMock.Setup(r => r.GetTeamChatMessagesAsync(tournamentId, userTeam.Id))
                .ReturnsAsync(teamMessages);
            repoMock.Setup(r => r.GetReportChatMessagesAsync(tournamentId, userTeam.Id, false))
                .ReturnsAsync(reportMessages);
            var userManager = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(u => u.FindByIdAsync("u1"))
                .ReturnsAsync(new ClashUser { Id = "u1", UserName = "User1" });
            userManager.Setup(u => u.FindByIdAsync("u2"))
                .ReturnsAsync(new ClashUser { Id = "u2", UserName = "User2" });
            userManager.Setup(u => u.FindByIdAsync("u3"))
                .ReturnsAsync(new ClashUser { Id = "u3", UserName = "User3" });
            var service = new ChatService(repoMock.Object, userManager.Object);

            // Act
            var model = await service.GetChatAsync(tournamentId, "user", false);

            // Assert
            Assert.NotNull(model);
            Assert.Equal(tournament, model!.Tournament);
            Assert.True(model.HasTeam);
            Assert.Equal(userTeam.Id, model.UserTeamId);
            Assert.Equal(allMessages, model.AllMessages);
            Assert.Equal(teamMessages, model.TeamMessages);
            Assert.Equal(reportMessages, model.ReportMessages);
            // User names dictionary should include all unique user ids
            Assert.Equal("User1", model.UserNames["u1"]);
            Assert.Equal("User2", model.UserNames["u2"]);
            Assert.Equal("User3", model.UserNames["u3"]);
        }

        [Fact]
        public async Task PostMessageAsync_DoesNotPost_WhenMessageWhitespace()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>(MockBehavior.Strict);
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostMessageAsync(1, "user", "   ", false);

            // Assert
            // No calls should be made to repository
            repoMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PostMessageAsync_DoesNotPost_WhenToTeamAndUserHasNoTeam()
        {
            // Arrange
            int tournamentId = 2;
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetUserTeamAsync(tournamentId, "user"))
                .ReturnsAsync((Team?)null);
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostMessageAsync(tournamentId, "user", "Hello", true);

            // Assert
            repoMock.Verify(r => r.AddChatMessageAsync(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task PostMessageAsync_PostsGeneralMessage_WhenValid()
        {
            // Arrange
            int tournamentId = 2;
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.AddChatMessageAsync(It.IsAny<ChatMessage>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostMessageAsync(tournamentId, "user", "Hello", false);

            // Assert
            repoMock.Verify();
            repoMock.Verify(r => r.AddChatMessageAsync(It.Is<ChatMessage>(m => m.TeamId == null && m.Message == "Hello" && m.TournamentId == tournamentId)), Times.Once);
        }

        [Fact]
        public async Task PostMessageAsync_PostsTeamMessage_WhenUserHasTeam()
        {
            // Arrange
            int tournamentId = 2;
            var team = new Team { Id = 10 };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetUserTeamAsync(tournamentId, "user"))
                .ReturnsAsync(team);
            repoMock.Setup(r => r.AddChatMessageAsync(It.IsAny<ChatMessage>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostMessageAsync(tournamentId, "user", "TeamMsg", true);

            // Assert
            repoMock.Verify();
            repoMock.Verify(r => r.AddChatMessageAsync(It.Is<ChatMessage>(m => m.TeamId == team.Id && m.Message == "TeamMsg")), Times.Once);
        }

        [Fact]
        public async Task PostReportMessageAsync_DoesNotPost_WhenMessageWhitespace()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>(MockBehavior.Strict);
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostReportMessageAsync(1, "user", "", null, true);

            // Assert
            repoMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PostReportMessageAsync_AdminPosts_WhenTeamIdProvided()
        {
            // Arrange
            int tournamentId = 2;
            int teamId = 5;
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.AddChatMessageAsync(It.IsAny<ChatMessage>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostReportMessageAsync(tournamentId, "admin", "Report", teamId, true);

            // Assert
            repoMock.Verify();
            repoMock.Verify(r => r.AddChatMessageAsync(It.Is<ChatMessage>(m => m.IsReport && m.TeamId == teamId && m.Message == "Report")), Times.Once);
        }

        [Fact]
        public async Task PostReportMessageAsync_AdminDoesNotPost_WhenNoTeamIdProvided()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>(MockBehavior.Strict);
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostReportMessageAsync(1, "admin", "Report", null, true);

            // Assert
            repoMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task PostReportMessageAsync_NonAdminPosts_WhenUserHasTeam()
        {
            // Arrange
            int tournamentId = 3;
            var team = new Team { Id = 7 };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetUserTeamAsync(tournamentId, "user"))
                .ReturnsAsync(team);
            repoMock.Setup(r => r.AddChatMessageAsync(It.IsAny<ChatMessage>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostReportMessageAsync(tournamentId, "user", "Report", null, false);

            // Assert
            repoMock.Verify();
            repoMock.Verify(r => r.AddChatMessageAsync(It.Is<ChatMessage>(m => m.TeamId == team.Id && m.IsReport && m.Message == "Report")), Times.Once);
        }

        [Fact]
        public async Task PostReportMessageAsync_NonAdminDoesNotPost_WhenUserHasNoTeam()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetUserTeamAsync(3, "user"))
                .ReturnsAsync((Team?)null);
            var service = new ChatService(repoMock.Object, CreateUserManager());

            // Act
            await service.PostReportMessageAsync(3, "user", "Report", null, false);

            // Assert
            repoMock.Verify(r => r.AddChatMessageAsync(It.IsAny<ChatMessage>()), Times.Never);
        }
    }
}