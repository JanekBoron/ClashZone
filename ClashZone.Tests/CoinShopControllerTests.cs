using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using ClashZone.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class CoinShopControllerTests
    {
        [Fact]
        public async Task Index_ReturnsCatalog_WithZeroBalanceForAnonymous()
        {
            var shop = new Mock<ICoinShopService>();
            var catalog = new List<ProductDomain> { new() { Id = 1, Name = "Cap" } };
            shop.Setup(s => s.GetCatalogAsync()).ReturnsAsync(catalog);
            var wallet = new Mock<ICoinWalletService>();

            var controller = new CoinShopController(shop.Object, wallet.Object);
            TestSupport.SetUser(controller, null, isAuthenticated: false);

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(catalog, view.Model);
            Assert.Equal(0, view.ViewData["Balance"]);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenProductMissing()
        {
            var shop = new Mock<ICoinShopService>();
            shop.Setup(s => s.GetProductAsync(7)).ReturnsAsync((ProductDomain?)null);
            var controller = new CoinShopController(shop.Object, new Mock<ICoinWalletService>().Object);
            TestSupport.SetUser(controller, null, isAuthenticated: false);

            var result = await controller.Details(7, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Checkout_Get_ReturnsForm_WithProductSummary()
        {
            var shop = new Mock<ICoinShopService>();
            shop.Setup(s => s.GetProductAsync(1)).ReturnsAsync(new ProductDomain { Id = 1, Name = "Cap", ClashCoins = 500, IsActive = true });
            var wallet = new Mock<ICoinWalletService>();
            wallet.Setup(w => w.GetBalanceAsync("u1")).ReturnsAsync(1000);
            var controller = new CoinShopController(shop.Object, wallet.Object);
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Checkout(1);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CheckoutViewModel>(view.Model);
            Assert.Equal(1, model.ProductId);
            Assert.Equal(500, model.ClashCoins);
            Assert.Equal(1000, model.Balance);
        }

        [Fact]
        public async Task Checkout_Post_SetsSuccessMessage_AndRedirectsToDetails()
        {
            var shop = new Mock<ICoinShopService>();
            shop.Setup(s => s.GetProductAsync(1)).ReturnsAsync(new ProductDomain { Id = 1, Name = "Cap", ClashCoins = 500, IsActive = true });
            shop.Setup(s => s.PurchaseAsync(1, "u1", It.IsAny<ShippingAddressDomain>())).ReturnsAsync((true, (string?)null));
            var wallet = new Mock<ICoinWalletService>();
            var controller = new CoinShopController(shop.Object, wallet.Object);
            TestSupport.SetUser(controller, "u1");

            var model = new CheckoutViewModel
            {
                ProductId = 1,
                FullName = "Jan Kowalski",
                Phone = "+48 600 100 200",
                Street = "ul. Testowa 1",
                PostalCode = "00-001",
                City = "Warszawa",
                Country = "Polska"
            };
            var result = await controller.Checkout(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("ShopOk"));
        }

        [Fact]
        public async Task Checkout_Post_SetsError_WhenPurchaseFails()
        {
            var shop = new Mock<ICoinShopService>();
            shop.Setup(s => s.GetProductAsync(1)).ReturnsAsync(new ProductDomain { Id = 1, Name = "Cap", ClashCoins = 500, IsActive = true });
            shop.Setup(s => s.PurchaseAsync(1, "u1", It.IsAny<ShippingAddressDomain>())).ReturnsAsync((false, "Za mało Clash Coins."));
            var controller = new CoinShopController(shop.Object, new Mock<ICoinWalletService>().Object);
            TestSupport.SetUser(controller, "u1");

            var model = new CheckoutViewModel
            {
                ProductId = 1,
                FullName = "Jan Kowalski",
                Phone = "+48 600 100 200",
                Street = "ul. Testowa 1",
                PostalCode = "00-001",
                City = "Warszawa",
                Country = "Polska"
            };
            var result = await controller.Checkout(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("ShopError"));
        }
    }
}
