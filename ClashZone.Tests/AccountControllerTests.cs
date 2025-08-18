using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="AccountController"/>.  These tests
    /// exercise the key authentication and account management flows including
    /// registration, login, password reset and email confirmation.  By
    /// mocking the <see cref="UserManager{TUser}"/>, <see cref="SignInManager{TUser}"/>
    /// and <see cref="IEmailService"/>, we isolate the controller logic and
    /// verify correct redirections, model state handling and email dispatch.
    /// </summary>
    public class AccountControllerTests
    {
        private static (AccountController controller, Mock<UserManager<ClashUser>> userMgrMock, Mock<SignInManager<ClashUser>> signInMgrMock, Mock<IEmailService> emailSvcMock) CreateController()
        {
            var userStore = new Mock<IUserStore<ClashUser>>();
            var userMgrMock = new Mock<UserManager<ClashUser>>(userStore.Object,
                null, null, null, null, null, null, null, null);
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var userClaimsFactory = new Mock<IUserClaimsPrincipalFactory<ClashUser>>();
            var opts = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
            opts.Setup(o => o.Value).Returns(new IdentityOptions());
            var signInMgrMock = new Mock<SignInManager<ClashUser>>(userMgrMock.Object,
                httpContextAccessor.Object,
                userClaimsFactory.Object,
                opts.Object,
                null, null, null);
            var emailSvcMock = new Mock<IEmailService>();
            var controller = new AccountController(userMgrMock.Object, signInMgrMock.Object, emailSvcMock.Object);
            // Setup TempData and Url
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            var urlHelperMock = new Mock<IUrlHelper>();
            // Provide default callback for Url.Action.  Tests may override.
            urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("http://callback");
            controller.Url = urlHelperMock.Object;
            return (controller, userMgrMock, signInMgrMock, emailSvcMock);
        }

        [Fact]
        public async Task Register_Success_SendsEmailAndRedirects()
        {
            // Arrange
            var (controller, userMgr, _, emailSvc) = CreateController();
            var model = new RegisterViewModel { UserName = "test", Email = "test@site.com", Password = "Pwd123$" };
            userMgr.Setup(m => m.CreateAsync(It.IsAny<ClashUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);
            userMgr.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ClashUser>()))
                .ReturnsAsync("token");
            // Act
            var result = await controller.Register(model);
            // Assert
            // Email should be sent
            emailSvc.Verify(e => e.SendEmailAsync(model.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            // Should redirect to Login
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.Login), redirect.ActionName);
            // TempData should contain success message
            Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        }

        [Fact]
        public async Task Register_Failure_ReturnsViewWithErrors()
        {
            // Arrange
            var (controller, userMgr, _, emailSvc) = CreateController();
            var model = new RegisterViewModel { UserName = "t", Email = "e", Password = "p" };
            var fail = IdentityResult.Failed(new IdentityError { Description = "fail" });
            userMgr.Setup(m => m.CreateAsync(It.IsAny<ClashUser>(), model.Password))
                .ReturnsAsync(fail);
            // Act
            var result = await controller.Register(model);
            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, view.Model);
            Assert.True(controller.ModelState.ErrorCount > 0);
            emailSvc.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsView()
        {
            var (controller, userMgr, _, _) = CreateController();
            controller.ModelState.AddModelError("UserName", "required");
            var model = new RegisterViewModel();
            var result = await controller.Register(model);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, view.Model);
            userMgr.Verify(m => m.CreateAsync(It.IsAny<ClashUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsView()
        {
            var (controller, _, _, _) = CreateController();
            controller.ModelState.AddModelError("Email", "required");
            var model = new LoginViewModel();
            var result = await controller.Login(model);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, view.Model);
        }

        [Fact]
        public async Task Login_UnconfirmedEmail_ReturnsViewWithMessage()
        {
            // Arrange
            var (controller, userMgr, _, _) = CreateController();
            var user = new ClashUser { UserName = "u", Email = "e" };
            var model = new LoginViewModel { Email = "e", Password = "p" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email.Trim())).ReturnsAsync(user);
            userMgr.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
            // Act
            var result = await controller.Login(model);
            // Assert
            var view = Assert.IsType<ViewResult>(result);
            // Should have model error for unconfirmed account
            Assert.True(controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Login_BannedUser_ReturnsViewWithMessage()
        {
            var (controller, userMgr, _, _) = CreateController();
            var user = new ClashUser { UserName = "u", Email = "e", IsBanned = true };
            var model = new LoginViewModel { Email = "e", Password = "p" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email.Trim())).ReturnsAsync(user);
            userMgr.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            // Act
            var result = await controller.Login(model);
            var view = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Login_Success_RedirectsToHome()
        {
            var (controller, userMgr, signInMgr, _) = CreateController();
            var user = new ClashUser { UserName = "u", Email = "e" };
            var model = new LoginViewModel { Email = "e", Password = "p" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email.Trim())).ReturnsAsync(user);
            userMgr.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            signInMgr.Setup(s => s.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            // Act
            var result = await controller.Login(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_Failed_ReturnsViewWithGenericMessage()
        {
            var (controller, userMgr, signInMgr, _) = CreateController();
            var user = new ClashUser { UserName = "u", Email = "e" };
            var model = new LoginViewModel { Email = "e", Password = "p" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email.Trim())).ReturnsAsync(user);
            userMgr.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            signInMgr.Setup(s => s.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
            var result = await controller.Login(model);
            var view = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task ForgotPassword_InvalidModel_ReturnsView()
        {
            var (controller, _, _, _) = CreateController();
            controller.ModelState.AddModelError("Email", "required");
            var model = new ForgotPasswordViewModel();
            var result = await controller.ForgotPassword(model);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, view.Model);
        }

        [Fact]
        public async Task ForgotPassword_UserNotFoundOrNotConfirmed_SetsSuccessAndRedirects()
        {
            var (controller, userMgr, _, _) = CreateController();
            var model = new ForgotPasswordViewModel { Email = "e" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync((ClashUser?)null);
            var result = await controller.ForgotPassword(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.ForgotPassword), redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        }

        [Fact]
        public async Task ForgotPassword_UserExistsAndConfirmed_SendsEmailAndRedirects()
        {
            var (controller, userMgr, _, emailSvc) = CreateController();
            var user = new ClashUser { UserName = "u", Email = "e" };
            var model = new ForgotPasswordViewModel { Email = "e" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            userMgr.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            userMgr.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");
            var result = await controller.ForgotPassword(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.ForgotPassword), redirect.ActionName);
            emailSvc.Verify(e => e.SendEmailAsync(model.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ResetPassword_Get_ReturnsBadRequest_WhenTokenMissing()
        {
            var (controller, _, _, _) = CreateController();
            var result = controller.ResetPassword(null!, "email");
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public void ResetPassword_Get_ReturnsView_WithModel()
        {
            var (controller, _, _, _) = CreateController();
            var result = controller.ResetPassword("token", "email");
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ResetPasswordViewModel>(view.Model);
            Assert.Equal("token", model.Token);
            Assert.Equal("email", model.Email);
        }

        [Fact]
        public async Task ResetPassword_Post_InvalidModel_ReturnsView()
        {
            var (controller, _, _, _) = CreateController();
            controller.ModelState.AddModelError("Password", "required");
            var model = new ResetPasswordViewModel();
            var result = await controller.ResetPassword(model);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, view.Model);
        }

        [Fact]
        public async Task ResetPassword_Post_UserNull_RedirectsAndSetsSuccess()
        {
            var (controller, userMgr, _, _) = CreateController();
            var model = new ResetPasswordViewModel { Email = "e", Token = "t", Password = "p" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync((ClashUser?)null);
            var result = await controller.ResetPassword(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.Login), redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        }

        [Fact]
        public async Task ResetPassword_Post_Success_RedirectsToLogin()
        {
            var (controller, userMgr, _, _) = CreateController();
            var user = new ClashUser { UserName = "u", Email = "e" };
            var model = new ResetPasswordViewModel { Email = "e", Token = "t", Password = "new" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            userMgr.Setup(m => m.ResetPasswordAsync(user, model.Token, model.Password)).ReturnsAsync(IdentityResult.Success);
            var result = await controller.ResetPassword(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.Login), redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        }

        [Fact]
        public async Task ResetPassword_Post_Fails_ReturnsViewWithErrors()
        {
            var (controller, userMgr, _, _) = CreateController();
            var user = new ClashUser { UserName = "u", Email = "e" };
            var model = new ResetPasswordViewModel { Email = "e", Token = "t", Password = "new" };
            userMgr.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            var fail = IdentityResult.Failed(new IdentityError { Description = "fail" });
            userMgr.Setup(m => m.ResetPasswordAsync(user, model.Token, model.Password)).ReturnsAsync(fail);
            var result = await controller.ResetPassword(model);
            var view = Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task ConfirmEmail_BadRequest_WhenParamsMissing()
        {
            var (controller, _, _, _) = CreateController();
            var result = await controller.ConfirmEmail("", "token");
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task ConfirmEmail_NotFound_WhenUserMissing()
        {
            var (controller, userMgr, _, _) = CreateController();
            userMgr.Setup(m => m.FindByIdAsync("uid")).ReturnsAsync((ClashUser?)null);
            var result = await controller.ConfirmEmail("uid", "token");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ConfirmEmail_Success_SetsSuccessMessageAndRedirects()
        {
            var (controller, userMgr, _, _) = CreateController();
            var user = new ClashUser { Id = "u" };
            userMgr.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
            userMgr.Setup(m => m.ConfirmEmailAsync(user, "token")).ReturnsAsync(IdentityResult.Success);
            var result = await controller.ConfirmEmail(user.Id, "token");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.Login), redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        }
    }
}