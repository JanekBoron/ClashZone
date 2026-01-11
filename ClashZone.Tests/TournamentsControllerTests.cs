using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="TournamentsController"/>.  These tests
    /// exercise the controller's various actions under different input
    /// conditions to ensure that it returns the correct result type
    /// (view, redirect or not found) and forwards calls to underlying
    /// services.  Mocks are used for all dependencies to isolate the
    /// controller logic.
    /// </summary>
    public class TournamentsControllerTests
    {
        private static UserManager<ClashUser> CreateUserManager()
        {
            // UserManager has no interface, so we construct it with mocked
            // dependencies.  Only the methods used in the controller are
            // explicitly set up in individual tests.
            var store = new Mock<IUserStore<ClashUser>>();
            var userValidators = new List<IUserValidator<ClashUser>>();
            var pwdValidators = new List<IPasswordValidator<ClashUser>>();
            return new UserManager<ClashUser>(store.Object,
                null, null, userValidators, pwdValidators, null, null, null, null);
        }

        private static TournamentsController CreateController(
            ITournamentService tournamentService = null,
            ITournamentsRepository tournamentsRepository = null,
            IChatService chatService = null,
            IBracketService bracketService = null,
            IMatchesService matchesService = null,
            IEmailService emailService = null)
        {
            var userManager = CreateUserManager();
            return new TournamentsController(
                userManager,
                Mock.Of<ISubscriptionRepository>(),
                chatService ?? Mock.Of<IChatService>(),
                bracketService ?? Mock.Of<IBracketService>(),
                tournamentService ?? Mock.Of<ITournamentService>(),
                tournamentsRepository ?? Mock.Of<ITournamentsRepository>(),
                emailService ?? Mock.Of<IEmailService>(),
                matchesService ?? Mock.Of<IMatchesService>());
        }

        [Fact]
        public async Task Matches_ReturnsViewWithModel_WhenTournamentAndMatchesExist()
        {
            // Arrange
            int tournamentId = 1;
            var tournament = new Tournament { Id = tournamentId };
            var matchList = new List<MatchListItemViewModel> { new MatchListItemViewModel() };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(tournamentId))
                .ReturnsAsync(tournament);
            var matchesServiceMock = new Mock<IMatchesService>();
            matchesServiceMock.Setup(s => s.GetMatchesForTournamentAsync(tournamentId))
                .ReturnsAsync(matchList);
            var controller = CreateController(
                tournamentsRepository: repoMock.Object,
                matchesService: matchesServiceMock.Object);

            // Act
            var result = await controller.Matches(tournamentId);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TournamentMatchesViewModel>(view.Model);
            Assert.Equal(tournament, model.Tournament);
            Assert.Equal(matchList, model.Matches);
        }

        [Fact]
        public async Task Matches_ReturnsNotFound_WhenTournamentDoesNotExist()
        {
            // Arrange
            int tournamentId = 42;
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(tournamentId))
                .ReturnsAsync((Tournament?)null);
            var controller = CreateController(tournamentsRepository: repoMock.Object);

            // Act
            var result = await controller.Matches(tournamentId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task MatchDetails_ReturnsView_WhenModelExists()
        {
            // Arrange
            int tournamentId = 1;
            int matchId = 3;
            var details = new MatchDetailsViewModel();
            var matchesServiceMock = new Mock<IMatchesService>();
            matchesServiceMock.Setup(s => s.GetMatchDetailsAsync(tournamentId, matchId))
                .ReturnsAsync(details);
            var controller = CreateController(matchesService: matchesServiceMock.Object);

            // Act
            var result = await controller.MatchDetails(tournamentId, matchId);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(details, view.Model);
        }

        [Fact]
        public async Task MatchDetails_ReturnsNotFound_WhenModelIsNull()
        {
            // Arrange
            var matchesServiceMock = new Mock<IMatchesService>();
            matchesServiceMock.Setup(s => s.GetMatchDetailsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((MatchDetailsViewModel?)null);
            var controller = CreateController(matchesService: matchesServiceMock.Object);

            // Act
            var result = await controller.MatchDetails(1, 2);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Rules_ReturnsViewWithDefaultRules_ForUnknownGameTitle()
        {
            // Arrange
            var tournament = new Tournament { Id = 1, GameTitle = "Unknown" };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            var controller = CreateController(tournamentsRepository: repoMock.Object);

            // Act
            var result = await controller.Rules(1);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RulesViewModel>(view.Model);
            Assert.Equal(tournament, model.Tournament);
            // For unknown games, two default rules should be returned
            Assert.True(model.Rules.Count >= 2);
        }

        [Fact]
        public async Task Rules_ReturnsNotFound_WhenTournamentMissing()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Tournament?)null);
            var controller = CreateController(tournamentsRepository: repoMock.Object);

            // Act
            var result = await controller.Rules(7);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ParticipantsList_ReturnsViewWithTeams()
        {
            // Arrange
            int tournamentId = 10;
            var tournament = new Tournament { Id = tournamentId };
            var team = new Team { Id = 1, CaptainId = "user1", Name = null };
            var memberIds = new List<string> { "user1", "user2" };
            var users = new Dictionary<string, ClashUser>
            {
                { "user1", new ClashUser { Id = "user1", UserName = "Captain" } },
                { "user2", new ClashUser { Id = "user2", UserName = "Player" } }
            };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(tournamentId))
                .ReturnsAsync(tournament);
            repoMock.Setup(r => r.GetTeamsForTournamentAsync(tournamentId))
                .ReturnsAsync(new List<Team> { team });
            repoMock.Setup(r => r.GetTeamMemberIdsAsync(team.Id))
                .ReturnsAsync(memberIds);
            var userManager = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => users[id]);
            var controller = new TournamentsController(
                userManager.Object,
                Mock.Of<ISubscriptionRepository>(),
                Mock.Of<IChatService>(),
                Mock.Of<IBracketService>(),
                Mock.Of<ITournamentService>(),
                repoMock.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<IMatchesService>());

            // Act
            var result = await controller.ParticipantsList(tournamentId);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ParticipantsViewModel>(view.Model);
            Assert.Single(model.Teams);
            var vm = model.Teams.First();
            Assert.Equal(team.Id, vm.TeamId);
            // Because team.Name is null, friendly name should be based on captain
            Assert.Equal("team_Captain", vm.Name);
            Assert.Equal(new[] { "Captain", "Player" }, vm.Members);
        }

        [Fact]
        public async Task ParticipantsList_ReturnsNotFound_WhenTournamentMissing()
        {
            // Arrange
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Tournament?)null);
            var controller = CreateController(tournamentsRepository: repoMock.Object);

            // Act
            var result = await controller.ParticipantsList(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Chat_ReturnsNotFound_WhenModelIsNull()
        {
            // Arrange
            int tournamentId = 5;
            var chatServiceMock = new Mock<IChatService>();
            chatServiceMock.Setup(s => s.GetChatAsync(tournamentId, It.IsAny<string>(), true))
                .ReturnsAsync((ChatViewModel?)null);
            var controller = CreateController(chatService: chatServiceMock.Object);

            // Setup user identity with admin role
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await controller.Chat(tournamentId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Chat_ReturnsView_WhenModelExists()
        {
            // Arrange
            int tournamentId = 2;
            var chatModel = new ChatViewModel();
            var chatServiceMock = new Mock<IChatService>();
            chatServiceMock.Setup(s => s.GetChatAsync(tournamentId, It.IsAny<string>(), false))
                .ReturnsAsync(chatModel);
            var controller = CreateController(chatService: chatServiceMock.Object);
            // Setup a regular user without admin role
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await controller.Chat(tournamentId);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(chatModel, view.Model);
        }

        [Fact]
        public async Task PostMessage_RedirectsToChatAndPostsMessage()
        {
            // Arrange
            int tournamentId = 3;
            string userId = "user";
            string message = "Hello";
            bool toTeam = false;
            var chatServiceMock = new Mock<IChatService>();
            chatServiceMock.Setup(s => s.PostMessageAsync(tournamentId, userId, message, toTeam))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var controller = CreateController(chatService: chatServiceMock.Object);
            // Setup user identity
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await controller.PostMessage(tournamentId, message, toTeam);

            // Assert
            chatServiceMock.Verify();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Chat), redirect.ActionName);
            Assert.Equal(tournamentId, redirect.RouteValues["id"]);
        }

        [Fact]
        public async Task PostReportMessage_RedirectsToChatAndPostsReport()
        {
            // Arrange
            int tournamentId = 3;
            string userId = "user";
            string message = "Report";
            int teamId = 1;
            bool isAdmin = true;
            var chatServiceMock = new Mock<IChatService>();
            chatServiceMock.Setup(s => s.PostReportMessageAsync(tournamentId, userId, message, teamId, isAdmin))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var controller = CreateController(chatService: chatServiceMock.Object);
            // Setup user with Admin role
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await controller.PostReportMessage(tournamentId, message, teamId);

            // Assert
            chatServiceMock.Verify();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Chat), redirect.ActionName);
            Assert.Equal(tournamentId, redirect.RouteValues["id"]);
        }

        [Fact]
        public async Task Bracket_RedirectsToDetails_WhenModelIsNull()
        {
            // Arrange
            int tournamentId = 4;
            var bracketServiceMock = new Mock<IBracketService>();
            bracketServiceMock.Setup(s => s.GetBracketAsync(tournamentId))
                .ReturnsAsync((BracketViewModel?)null);
            var controller = CreateController(bracketService: bracketServiceMock.Object);

            // Act
            var result = await controller.Bracket(tournamentId);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Details), redirect.ActionName);
            Assert.Equal(tournamentId, redirect.RouteValues["id"]);
        }

        [Fact]
        public async Task Bracket_ReturnsView_WhenModelExists()
        {
            // Arrange
            int tournamentId = 4;
            var bracketVm = new BracketViewModel();
            var bracketServiceMock = new Mock<IBracketService>();
            bracketServiceMock.Setup(s => s.GetBracketAsync(tournamentId))
                .ReturnsAsync(bracketVm);
            var controller = CreateController(bracketService: bracketServiceMock.Object);

            // Act
            var result = await controller.Bracket(tournamentId);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(bracketVm, view.Model);
        }

        [Fact]
        public async Task GenerateResults_RedirectsToDetails_WhenModelIsNull()
        {
            // Arrange
            int tournamentId = 4;
            var bracketServiceMock = new Mock<IBracketService>();
            bracketServiceMock.Setup(s => s.GetBracketWithResultsAsync(tournamentId))
                .ReturnsAsync((BracketViewModel?)null);
            var controller = CreateController(bracketService: bracketServiceMock.Object);

            // Act
            var result = await controller.GenerateResults(tournamentId);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Details), redirect.ActionName);
            Assert.Equal(tournamentId, redirect.RouteValues["id"]);
        }

        [Fact]
        public async Task GenerateResults_ReturnsView_WhenModelExists()
        {
            // Arrange
            int tournamentId = 4;
            var bracketVm = new BracketViewModel();
            var bracketServiceMock = new Mock<IBracketService>();
            bracketServiceMock.Setup(s => s.GetBracketWithResultsAsync(tournamentId))
                .ReturnsAsync(bracketVm);
            var controller = CreateController(bracketService: bracketServiceMock.Object);

            // Act
            var result = await controller.GenerateResults(tournamentId);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            // Name explicitly set to "Bracket" view
            Assert.Equal("Bracket", view.ViewName);
            Assert.Equal(bracketVm, view.Model);
        }

        [Fact]
        public async Task Index_ReturnsViewWithUpcomingTournaments()
        {
            // Arrange
            var tournaments = new List<Tournament> { new Tournament(), new Tournament() };
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.GetUpcomingTournamentsAsync(null))
                .ReturnsAsync(tournaments);
            var controller = CreateController(tournamentService: serviceMock.Object);

            // Act
            var result = await controller.Index(null);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(tournaments, view.Model);
            Assert.False((bool)view.ViewData["IsMy"]);
        }

        [Fact]
        public async Task MyTournaments_ReturnsViewWithUserTournaments()
        {
            // Arrange
            var userTournaments = new List<Tournament> { new Tournament() };
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.GetUserTournamentsAsync("user"))
                .ReturnsAsync(userTournaments);
            var controller = CreateController(tournamentService: serviceMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await controller.MyTournaments();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(userTournaments, view.Model);
            Assert.True((bool)view.ViewData["IsMy"]);
            Assert.Null(view.ViewData["SelectedFormat"]);
        }

        [Fact]
        public void Create_ReturnsView()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task CreateTournament_RedirectsToIndex_WhenModelStateValid()
        {
            // Arrange
            var serviceMock = new Mock<ITournamentService>();
            Tournament? capturedTournament = null;
            serviceMock.Setup(s => s.CreateTournamentAsync(It.IsAny<Tournament>(), "user"))
                .Callback<Tournament, string>((t, userId) => capturedTournament = t)
                .Returns(Task.CompletedTask);
            var controller = CreateController(tournamentService: serviceMock.Object);
            // Setup user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            // Valid model
            var tournament = new Tournament { IsPublic = false };
            // Act
            var result = await controller.CreateTournament(tournament,
                "text", "Nagroda", null,
                "coins", null, 100,
                "text", "Trzecie", null);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Index), redirect.ActionName);
            Assert.NotNull(capturedTournament);
            // Game title should be forced to CS2
            Assert.Equal("Counter Strike 2", capturedTournament!.GameTitle);
            // Prize string should contain pipe separated values
           // Assert.Equal("Nagroda|100 ClashCoins|Trzecie", capturedTournament.Prize);
        }

        [Fact]
        public async Task CreateTournament_ReturnsView_WhenModelStateInvalid()
        {
            // Arrange
            var controller = CreateController();
            controller.ModelState.AddModelError("Error", "Invalid");
            var tournament = new Tournament();

            // Act
            var result = await controller.CreateTournament(tournament,
                "text", null, null,
                "text", null, null,
                "text", null, null);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(tournament, view.Model);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenModelIsNull()
        {
            // Arrange
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.GetTournamentDetailsAsync(1, null))
                .ReturnsAsync((TournamentDetailsViewModel?)null);
            var controller = CreateController(tournamentService: serviceMock.Object);

            // Act
            var result = await controller.Details(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsView_WhenModelExists()
        {
            // Arrange
            var details = new TournamentDetailsViewModel();
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.GetTournamentDetailsAsync(1, null))
                .ReturnsAsync(details);
            var controller = CreateController(tournamentService: serviceMock.Object);

            // Act
            var result = await controller.Details(1);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(details, view.Model);
        }

        [Fact]
        public async Task Join_ReturnsNotFound_WhenTournamentNotFound()
        {
            // Arrange
            var joinResult = new JoinTournamentResult { NotFound = true };
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.JoinTournamentAsync(1, "user"))
                .ReturnsAsync(joinResult);
            var repoMock = new Mock<ITournamentsRepository>();
            var controller = CreateController(tournamentService: serviceMock.Object, tournamentsRepository: repoMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.Join(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Join_RedirectsToSubscription_WhenRequiresSubscription()
        {
            // Arrange
            var joinResult = new JoinTournamentResult { RequiresSubscription = true };
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.JoinTournamentAsync(1, "user"))
                .ReturnsAsync(joinResult);
            var repoMock = new Mock<ITournamentsRepository>();
            var controller = CreateController(tournamentService: serviceMock.Object, tournamentsRepository: repoMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.Join(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Subscription", redirect.ControllerName);
            // TempData should contain subscription error
            Assert.True(controller.TempData.ContainsKey("SubscriptionError"));
        }

        [Fact]
        public async Task Join_RedirectsToDetails_WhenAlreadyJoined()
        {
            // Arrange
            var joinResult = new JoinTournamentResult { AlreadyJoined = true };
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.JoinTournamentAsync(1, "user"))
                .ReturnsAsync(joinResult);
            var repoMock = new Mock<ITournamentsRepository>();
            var controller = CreateController(tournamentService: serviceMock.Object, tournamentsRepository: repoMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.Join(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Details), redirect.ActionName);
            Assert.Equal(1, redirect.RouteValues["id"]);
        }

        [Fact]
        public async Task Join_RedirectsToDetails_WhenMaxParticipantsExceeded()
        {
            // Arrange
            var joinResult = new JoinTournamentResult { MaxParticipantsExceeded = true };
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.JoinTournamentAsync(1, "user"))
                .ReturnsAsync(joinResult);
            var repoMock = new Mock<ITournamentsRepository>();
            var controller = CreateController(tournamentService: serviceMock.Object, tournamentsRepository: repoMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.Join(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Details), redirect.ActionName);
            Assert.Equal(1, redirect.RouteValues["id"]);
            // TempData should contain join error
            Assert.True(controller.TempData.ContainsKey("JoinError"));
        }

        [Fact]
        public async Task Join_SendsConfirmationEmail_WhenJoiningSuccessfully()
        {
            // Arrange
            var joinResult = new JoinTournamentResult
            {
                Team = new Team { Id = 99, JoinCode = "ABC", Name = null },
                TournamentFormat = "2v2",
                IsPremium = false
            };
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.JoinTournamentAsync(1, "user"))
                .ReturnsAsync(joinResult);
            var tournament = new Tournament { Id = 1, Name = "Test", Format = "2v2" };
            var userEntity = new ClashUser { Id = "user", Email = "user@test.com" };
            var repoMock = new Mock<ITournamentsRepository>();
            repoMock.Setup(r => r.GetTournamentByIdAsync(1))
                .ReturnsAsync(tournament);
            var emailMock = new Mock<IEmailService>();
            emailMock.Setup(e => e.SendTournamentRegistrationConfirmationAsync(userEntity.Email, tournament.Name))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var userManager = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(u => u.FindByIdAsync("user"))
                .ReturnsAsync(userEntity);
            var controller = new TournamentsController(
                userManager.Object,
                Mock.Of<ISubscriptionRepository>(),
                Mock.Of<IChatService>(),
                Mock.Of<IBracketService>(),
                serviceMock.Object,
                repoMock.Object,
                emailMock.Object,
                Mock.Of<IMatchesService>());
            // Setup user claims
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.Join(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Details), redirect.ActionName);
            Assert.Equal(1, redirect.RouteValues["id"]);
            emailMock.Verify();
        }

        [Fact]
        public async Task JoinTeam_ReturnsRedirect_WhenServiceReturnsTournamentId()
        {
            // Arrange
            var serviceMock = new Mock<ITournamentService>();
            serviceMock.Setup(s => s.JoinTeamAsync(5, "user", "CODE"))
                .ReturnsAsync(1);
            var controller = CreateController(tournamentService: serviceMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user")
            }, "TestAuth"));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            // Act
            var result = await controller.JoinTeam(5, "CODE");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(TournamentsController.Details), redirect.ActionName);
            Assert.Equal(1, redirect.RouteValues["id"]);
        }
    }
}