using ClashZone.Controllers;
using ClashZone;
using ClashZone.DataAccess.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using ClashZone.DataAccess.Models;

namespace ClashZone.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="PaymentController"/>.  The controller
    /// coordinates Stripe payments and subscription creation.  Because the
    /// controller creates its own <c>SessionService</c> internally for
    /// interacting with Stripe, these tests focus on the actions that
    /// participate in local side effects: creating subscriptions and
    /// surfacing cancellation messages.
    /// </summary>
    public class PaymentControllerTests
    {
        /// <summary>
        /// Tests that the <see cref="PaymentController.Success(int)"/>
        /// action creates a subscription for the current user and then
        /// redirects to the subscription listing page.  The user identity
        /// is provided via the controller's <see cref="ControllerContext"/>,
        /// and the repository is mocked to verify invocation.
        /// </summary>
        [Fact]
        public async Task Success_CreatesSubscriptionAndRedirects()
        {
            // Arrange
            var userId = "user123";
            var planId = 42;
            var repoMock = new Mock<ISubscriptionRepository>(MockBehavior.Strict);
            repoMock.Setup(r => r.CreateSubscriptionAsync(userId, planId))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

            // Provide dummy Stripe settings via IOptions.  The actual values
            // are irrelevant for this test because the controller does not
            // use them beyond storing them.
            var stripeOptionsMock = new Mock<IOptions<StripeSettings>>();
            stripeOptionsMock.Setup(o => o.Value).Returns(new StripeSettings());

            var controller = new PaymentController(repoMock.Object, stripeOptionsMock.Object);
            // Create a ClaimsPrincipal with the expected NameIdentifier claim.
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) });
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await controller.Success(planId);

            // Assert
            repoMock.Verify();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Subscription", redirect.ControllerName);
        }

        /// <summary>
        /// Tests that the <see cref="PaymentController.Cancel"/> action
        /// populates an error message in <see cref="Controller.TempData"/>
        /// and redirects to the subscription index.  This ensures users see
        /// cancellation feedback after aborting a payment.
        /// </summary>
        [Fact]
        public void Cancel_SetsTempDataAndRedirects()
        {
            // Arrange
            var repoMock = new Mock<ISubscriptionRepository>(MockBehavior.Strict);
            // Provide dummy Stripe settings to satisfy the constructor
            var stripeOptionsMock = new Mock<IOptions<StripeSettings>>();
            stripeOptionsMock.Setup(o => o.Value).Returns(new StripeSettings());
            var controller = new PaymentController(repoMock.Object, stripeOptionsMock.Object);

            // Set up TempData using a temp data dictionary provider.  We need
            // a non-null provider even if it is not used by the controller.
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            // Act
            var result = controller.Cancel();

            // Assert
            Assert.True(controller.TempData.ContainsKey("SubscriptionError"));
            Assert.Equal("Transakcja została anulowana.", controller.TempData["SubscriptionError"]);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Subscription", redirect.ControllerName);
        }
    }
}