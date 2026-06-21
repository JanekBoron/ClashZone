using System.Threading.Tasks;
using ClashZone.App.Ports.DataAccess;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class PaymentControllerTests
    {
        private static PaymentController CreateController(Mock<ISubscriptionRepository> repo)
            => new PaymentController(repo.Object, Options.Create(new StripeSettings()));

        [Fact]
        public async Task Success_CreatesSubscription_AndRedirectsToSubscriptionIndex()
        {
            var repo = new Mock<ISubscriptionRepository>();
            var controller = CreateController(repo);
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Success(planId: 2);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Subscription", redirect.ControllerName);
            repo.Verify(r => r.CreateSubscriptionAsync("u1", 2), Times.Once);
        }

        [Fact]
        public void Cancel_SetsErrorMessage_AndRedirects()
        {
            var repo = new Mock<ISubscriptionRepository>();
            var controller = CreateController(repo);
            TestSupport.SetUser(controller, "u1");

            var result = controller.Cancel();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("SubscriptionError"));
        }
    }
}
