using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Services.Interfaces;
using ClashZone.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="CoinShopController"/> — katalog, szczegóły produktu i zakup.
    /// </summary>
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
        public async Task Buy_SetsSuccessMessage_AndRedirectsToDetails()
        {
            var shop = new Mock<ICoinShopService>();
            shop.Setup(s => s.PurchaseAsync(1, "u1")).ReturnsAsync((true, (string?)null));
            var controller = new CoinShopController(shop.Object, new Mock<ICoinWalletService>().Object);
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Buy(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("ShopOk"));
        }

        [Fact]
        public async Task Buy_SetsError_WhenPurchaseFails()
        {
            var shop = new Mock<ICoinShopService>();
            shop.Setup(s => s.PurchaseAsync(1, "u1")).ReturnsAsync((false, "Za mało Clash Coins."));
            var controller = new CoinShopController(shop.Object, new Mock<ICoinWalletService>().Object);
            TestSupport.SetUser(controller, "u1");

            var result = await controller.Buy(1);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.True(controller.TempData.ContainsKey("ShopError"));
        }
    }
}
