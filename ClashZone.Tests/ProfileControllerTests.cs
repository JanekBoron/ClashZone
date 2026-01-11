using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.ViewModels;
using DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ClashZone.Tests { }
//{
  /*  /// <summary>
    /// Tests for the <see cref="ProfileController"/>.  Due to the complexity of
    /// the edit workflow this suite focuses on the core flows such as loading
    /// the profile, redirecting when the user cannot be resolved and basic
    /// profile edits without file uploads or password changes.  EF Core
    /// in-memory provider is used for the application context.
    /// </summary>
    public class ProfileControllerTests
    {
        private static (ProfileController controller, Mock<UserManager<ClashUser>> userMgrMock, Mock<SignInManager<ClashUser>> signInMock, ApplicationDbContext context) CreateControllerWithContext(ClashUser? user, UserStat? stat = null)
        {
            // In-memory EF context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            if (stat != null)
            {
                context.UserStats.Add(stat);
                context.SaveChanges();
            }
            var userStore = Mock.Of<IUserStore<ClashUser>>();
            var userMgrMock = new Mock<UserManager<ClashUser>>(userStore, null, null, null, null, null, null, null, null);
            userMgrMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            var signInStore = Mock.Of<IUserStore<ClashUser>>();
            var contextAccessor = Mock.Of<IHttpContextAccessor>();
            var userClaimsPrincipalFactory = Mock.Of<IUserClaimsPrincipalFactory<ClashUser>>();
            var signInOptions = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
            signInOptions.Setup(o => o.Value).Returns(new IdentityOptions());
            var signInMock = new Mock<SignInManager<ClashUser>>(userMgrMock.Object, contextAccessor, userClaimsPrincipalFactory, signInOptions.Object, null, null, null);
            var subRepoMock = new Mock<ISubscriptionRepository>();
            // Environment
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.SetupGet(e => e.WebRootPath).Returns(Path.GetTempPath());
            var controller = new ProfileController(userMgrMock.Object, signInMock.Object, subRepoMock.Object, context, envMock.Object);
            var httpContext = new DefaultHttpContext();
            if (user != null)
            {
                var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "mock");
                httpContext.User = new ClaimsPrincipal(identity);
            }
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return (controller, userMgrMock, signInMock, context);
        }

        [Fact]
        public async Task Index_RedirectsToLogin_WhenUserNull()
        {
            // Arrange
            var (controller, _, _, _) = CreateControllerWithContext(null);
            // Act
            var result = await controller.Index();
            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Index_ReturnsViewWithModel_WhenUserExists()
        {
            // Arrange
            var user = new ClashUser { Id = "u", UserName = "user" };
            var stat = new UserStat { UserId = "u", MatchesPlayed = 10, TotalAssists = 20, TotalDeaths = 5 };
            var subscription = new SubscriptionPlan { Id = 1, Name = "Plan" };
            var (controller, userMgrMock, _, context) = CreateControllerWithContext(user, stat);
            var subRepoMock = new Mock<ISubscriptionRepository>();
            subRepoMock.Setup(r => r.GetActiveSubscriptionAsync("u")).ReturnsAsync((UserSubscription?)null);
            // Replace subscription repo in controller
            typeof(ProfileController).GetField("_subscriptionRepository", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(controller, subRepoMock.Object);

            // Act
            var result = await controller.Index();
            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProfileViewModel>(view.Model);
            Assert.Equal(user, model.User);
            Assert.Equal(stat, model.Statistics);
            Assert.Null(model.Subscription);
        }

        [Fact]
        public async Task Edit_Get_RedirectsToLogin_WhenUserNull()
        {
            // Arrange
            var (controller, _, _, _) = CreateControllerWithContext(null);
            // Act
            var result = await controller.Edit();
            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Edit_Get_ReturnsPrepopulatedModel_WhenUserExists()
        {
            // Arrange
            var user = new ClashUser { Id = "u", UserName = "user", Email = "e@mail.com", DisplayName = "Display" };
            var (controller, _, _, _) = CreateControllerWithContext(user);
            // Act
            var result = await controller.Edit();
            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditProfileViewModel>(view.Model);
            Assert.Equal(user.UserName, model.UserName);
            Assert.Equal(user.Email, model.Email);
            Assert.Equal(user.DisplayName, model.DisplayName);
        }

        [Fact]
        public async Task Edit_Post_RedirectsToLogin_WhenUserNull()
        {
            // Arrange
            var (controller, _, _, _) = CreateControllerWithContext(null);
            var model = new EditProfileViewModel { UserName = "u", Email = "e" };
            // Act
            var result = await controller.Edit(model);
            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Edit_Post_Success_NoChanges_RedirectsToIndex()
        {
            // Arrange
            var user = new ClashUser { Id = "u", UserName = "user", Email = "e@mail.com", DisplayName = "Display" };
            var (controller, userMgrMock, signInMock, _) = CreateControllerWithContext(user);
            var model = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                DisplayName = "NewDisplay"
            };
            // Setup update to succeed
            userMgrMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            // Act
            var result = await controller.Edit(model);
            // Assert
            userMgrMock.Verify(m => m.SetUserNameAsync(It.IsAny<ClashUser>(), It.IsAny<string>()), Times.Never);
            userMgrMock.Verify(m => m.SetEmailAsync(It.IsAny<ClashUser>(), It.IsAny<string>()), Times.Never);
            userMgrMock.Verify(m => m.UpdateAsync(user), Times.Once);
            signInMock.Verify(s => s.RefreshSignInAsync(user), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ProfileController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task Edit_Post_NewPasswordWithoutCurrent_ReturnsError()
        {
            // Arrange
            var user = new ClashUser { Id = "u", UserName = "user", Email = "e" };
            var (controller, userMgrMock, _, _) = CreateControllerWithContext(user);
            var model = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                NewPassword = "newpass",
                ConfirmPassword = "newpass",
                CurrentPassword = null
            };
            // Act
            var result = await controller.Edit(model);
            // Assert
            var view = Assert.IsType<ViewResult>(result);
            // Should remain the model; error should have been added
            Assert.Equal(model, view.Model);
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
        }
    }
}*/