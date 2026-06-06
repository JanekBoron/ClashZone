using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="SubscriptionService"/> — pobieranie planów oraz zakup.
    /// </summary>
    public class SubscriptionServiceTests
    {
        [Fact]
        public async Task GetSubscriptionIndexViewModelAsync_ReturnsPlans()
        {
            var repo = new Mock<ISubscriptionRepository>();
            repo.Setup(r => r.GetAllPlansAsync()).ReturnsAsync(new[]
            {
                new SubscriptionPlanDomain { Id = 1, Name = "Premium" },
                new SubscriptionPlanDomain { Id = 2, Name = "Ultra" }
            });
            repo.Setup(r => r.GetActiveSubscriptionAsync("u1")).ReturnsAsync((UserSubscriptionDomain?)null);

            var model = await new SubscriptionService(repo.Object).GetSubscriptionIndexViewModelAsync("u1");

            Assert.Equal(2, model.Plans.Count);
        }

        [Fact]
        public async Task PurchaseSubscriptionAsync_DelegatesToRepository()
        {
            var repo = new Mock<ISubscriptionRepository>();

            await new SubscriptionService(repo.Object).PurchaseSubscriptionAsync(3, "u1");

            repo.Verify(r => r.CreateSubscriptionAsync("u1", 3), Times.Once);
        }
    }
}
