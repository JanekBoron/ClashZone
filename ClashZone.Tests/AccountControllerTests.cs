using System.Threading.Tasks;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="AccountController"/> — logowanie i rejestracja
    /// z mockowanym Identity (UserManager/SignInManager).
    /// </summary>
    public class AccountControllerTests
    {
        private static AccountController CreateController(
            Mock<UserManager<ClashUser>> userManager,
            Mock<SignInManager<ClashUser>>? signInManager = null,
            Mock<IEmailService>? email = null)
        {
            var sm = signInManager ?? TestSupport.MockSignInManager(userManager.Object);
            return new AccountController(userManager.Object, sm.Object, (email ?? new Mock<IEmailService>()).Object);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsView()
        {
            var um = TestSupport.MockUserManager();
            var controller = CreateController(um);
            controller.ModelState.AddModelError("Email", "required");

            var result = await controller.Login(new LoginViewModel());

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_UnknownEmail_AddsGenericError_AndDoesNotSignIn()
        {
            var um = TestSupport.MockUserManager();
            um.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ClashUser?)null);
            var sm = TestSupport.MockSignInManager(um.Object);
            var controller = CreateController(um, sm);

            var result = await controller.Login(new LoginViewModel { Email = "x@x.pl", Password = "p" });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            sm.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Login_ValidConfirmedUser_RedirectsToHome()
        {
            var user = new ClashUser { Id = "1", UserName = "alice", Email = "a@x.pl", IsBanned = false };
            var um = TestSupport.MockUserManager();
            um.Setup(m => m.FindByEmailAsync("a@x.pl")).ReturnsAsync(user);
            um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            var sm = TestSupport.MockSignInManager(um.Object);
            sm.Setup(s => s.PasswordSignInAsync("alice", "p", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            var controller = CreateController(um, sm);

            var result = await controller.Login(new LoginViewModel { Email = "a@x.pl", Password = "p" });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Register_DuplicateUserName_AddsError_AndDoesNotCreate()
        {
            var um = TestSupport.MockUserManager();
            um.Setup(m => m.FindByNameAsync("taken")).ReturnsAsync(new ClashUser { UserName = "taken" });
            var controller = CreateController(um);

            var result = await controller.Register(new RegisterViewModel { UserName = "taken", Email = "a@x.pl", Password = "P@ssw0rd!" });

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            um.Verify(m => m.CreateAsync(It.IsAny<ClashUser>(), It.IsAny<string>()), Times.Never);
        }
    }
}
