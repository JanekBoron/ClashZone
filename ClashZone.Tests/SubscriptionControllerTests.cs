using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.ViewModels;
using ClashZone.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="SubscriptionController"/> — lista planów i zakup.
    /// </summary>
    public class SubscriptionControllerTests
    {
        [Fact]
        public async Task Index_ReturnsViewModelWithPlansAndActiveSubscription()
        {
            var repo = new Mock<ISubscriptionRepository>();
            repo.Setup(r => r.GetAllPlansAsync()).ReturnsAsync(new[]
            {
                new SubscriptionPlanDomain { Id = 1, Name = "Premium" }
            });
            repo.Setup(r => r.GetActiveSubscriptionAsync("u1"))
                .ReturnsAsync(new UserSubscriptionDomain { Id = 9, UserId = "u1" });

            var controller = new SubscriptionController(repo.Object);
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Index();

            var model = Assert.IsType<SubscriptionIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Plans);
            Assert.NotNull(model.ActiveSubscription);
        }

        [Fact]
        public async Task Purchase_CreatesSubscription_AndRedirects()
        {
            var repo = new Mock<ISubscriptionRepository>();
            var controller = new SubscriptionController(repo.Object);
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Purchase(3);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            repo.Verify(r => r.CreateSubscriptionAsync("u1", 3), Times.Once);
        }
    }
}
