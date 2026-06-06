using System.Collections.Generic;
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
    /// Testy <see cref="OrdersController"/> (panel admina) — lista zamówień z
    /// loginami oraz zmiana statusu (walidacja i delegacja do repozytorium).
    /// </summary>
    public class OrdersControllerTests
    {
        private static OrdersController CreateController(Mock<IProductRedeemRepository> orders, Mock<IUserRepository>? users = null)
            => new OrdersController(orders.Object, (users ?? new Mock<IUserRepository>()).Object);

        [Fact]
        public async Task Index_BuildsItems_WithResolvedUserNames()
        {
            var orders = new Mock<IProductRedeemRepository>();
            orders.Setup(o => o.GetAllAsync()).ReturnsAsync(new List<ProductRedeemDomain>
            {
                new() { Id = 1, UserId = "u1", Status = OrderStatus.Pending, Product = new ProductDomain { Name = "Cap", ClashCoins = 100 } },
                new() { Id = 2, UserId = "u1", Status = OrderStatus.Completed, Product = new ProductDomain { Name = "Hat", ClashCoins = 50 } }
            });
            var users = new Mock<IUserRepository>();
            users.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(new ClashUserDomain { Id = "u1", UserName = "alice" });

            var result = await CreateController(orders, users).Index();

            var model = Assert.IsAssignableFrom<IEnumerable<OrderListItemViewModel>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Collection(model,
                i => { Assert.Equal("alice", i.UserName); Assert.Equal("Cap", i.ProductName); },
                i => { Assert.Equal("alice", i.UserName); Assert.Equal("Hat", i.ProductName); });
            // Login resolved once and cached for repeated user id.
            users.Verify(u => u.FindByIdAsync("u1"), Times.Once);
        }

        [Fact]
        public async Task UpdateStatus_InvalidStatus_DoesNotUpdate()
        {
            var orders = new Mock<IProductRedeemRepository>();
            var controller = CreateController(orders);
            TestSupport.SetUser(controller, "admin", roles: "Admin");

            var result = await controller.UpdateStatus(1, "Bogus", null);

            Assert.IsType<RedirectToActionResult>(result);
            orders.Verify(o => o.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatus_ValidStatus_UpdatesAndRedirects()
        {
            var orders = new Mock<IProductRedeemRepository>();
            var controller = CreateController(orders);
            TestSupport.SetUser(controller, "admin", roles: "Admin");

            var result = await controller.UpdateStatus(7, OrderStatus.Completed, "wysłano");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            orders.Verify(o => o.UpdateStatusAsync(7, OrderStatus.Completed, "wysłano"), Times.Once);
        }
    }
}
