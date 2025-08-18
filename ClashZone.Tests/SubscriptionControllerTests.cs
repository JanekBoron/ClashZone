using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Tests for <see cref="SubscriptionController"/>.  The controller
    /// displays available subscription plans and creates subscriptions on
    /// purchase.  TempData messages are surfaced via ViewBag on the index.
    /// </summary>
    public class SubscriptionControllerTests
    {
        private static SubscriptionController CreateController(ISubscriptionRepository repo, UserManager<ClashUser> userManager, ClaimsPrincipal? user = null)
        {
            var controller = new SubscriptionController(repo, userManager);
            var httpContext = new DefaultHttpContext();
            if (user != null)
            {
                httpContext.User = user;
            }
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        [Fact]
        public async Task Index_ReturnsView_WithPlansAndActiveSubscriptionAndError()
        {
            // Arrange
            var plans = new List<SubscriptionPlan>
    {
        new SubscriptionPlan { Id = 1, Name = "Free" }
    };
            var active = new UserSubscription { Id = 1, PlanId = 1 };

            var repoMock = new Mock<ISubscriptionRepository>();

            repoMock
                .Setup(r => r.GetAllPlansAsync())
                .ReturnsAsync(plans.ToArray());

            repoMock
                .Setup(r => r.GetActiveSubscriptionAsync("u"))
                .ReturnsAsync(active);

            var userMgrMock = new Mock<UserManager<ClashUser>>(
                Mock.Of<IUserStore<ClashUser>>(),
                null, null, null, null, null, null, null, null);

            userMgrMock
                .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns("u");

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "u") }, "mock"));

            var controller = CreateController(repoMock.Object, userMgrMock.Object, principal);
            controller.TempData["SubscriptionError"] = "err";

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            dynamic model = view.Model!;

            // Rzutujemy na IEnumerable by nie miec problemu list vs array
            var modelPlans = ((IEnumerable<SubscriptionPlan>)model.Plans).ToList();
            Assert.Single(modelPlans);
            Assert.Equal(1, modelPlans[0].Id);
            Assert.Equal("Free", modelPlans[0].Name);

            Assert.Equal(active, model.ActiveSubscription);
            Assert.Equal("err", controller.ViewBag.SubscriptionError);
        }

        [Fact]
        public async Task Purchase_CreatesSubscriptionAndRedirects()
        {
            // Arrange
            var repoMock = new Mock<ISubscriptionRepository>();
            var userMgrMock = new Mock<UserManager<ClashUser>>(Mock.Of<IUserStore<ClashUser>>(), null, null, null, null, null, null, null, null);
            userMgrMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("uid");
            var controller = CreateController(repoMock.Object, userMgrMock.Object, new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "uid") }, "mock")));

            // Act
            var result = await controller.Purchase(5);

            // Assert
            repoMock.Verify(r => r.CreateSubscriptionAsync("uid", 5), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(SubscriptionController.Index), redirect.ActionName);
        }
    }
}