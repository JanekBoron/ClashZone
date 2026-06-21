using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class CoinShopServiceTests
    {
        private static CoinShopService CreateService(
            Mock<IProductsRepository> products,
            Mock<ICoinWalletRepository> wallets,
            Mock<IProductRedeemRepository>? redeems = null)
            => new CoinShopService(products.Object, wallets.Object, (redeems ?? new Mock<IProductRedeemRepository>()).Object);

        private static ShippingAddressDomain SampleAddress() => new()
        {
            FullName = "Jan Kowalski",
            Phone = "+48 600 100 200",
            Street = "ul. Testowa 1",
            PostalCode = "00-001",
            City = "Warszawa",
            Country = "Polska"
        };

        [Fact]
        public async Task PurchaseAsync_Fails_WhenProductInactiveOrMissing()
        {
            var products = new Mock<IProductsRepository>();
            products.Setup(p => p.GetAsync(1)).ReturnsAsync((ProductDomain?)null);

            var (ok, error) = await CreateService(products, new Mock<ICoinWalletRepository>()).PurchaseAsync(1, "u1", SampleAddress());

            Assert.False(ok);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task PurchaseAsync_Fails_WhenInsufficientBalance()
        {
            var products = new Mock<IProductsRepository>();
            products.Setup(p => p.GetAsync(1)).ReturnsAsync(new ProductDomain { Id = 1, IsActive = true, ClashCoins = 100, Stock = 5 });
            var wallets = new Mock<ICoinWalletRepository>();
            wallets.Setup(w => w.GetBalanceAsync("u1")).ReturnsAsync(50);

            var (ok, error) = await CreateService(products, wallets).PurchaseAsync(1, "u1", SampleAddress());

            Assert.False(ok);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task PurchaseAsync_Succeeds_RecordsTransactionRedeemAndDecrementsStock()
        {
            var product = new ProductDomain { Id = 1, IsActive = true, ClashCoins = 100, Stock = 5 };
            var products = new Mock<IProductsRepository>();
            products.Setup(p => p.GetAsync(1)).ReturnsAsync(product);
            var wallets = new Mock<ICoinWalletRepository>();
            wallets.Setup(w => w.GetBalanceAsync("u1")).ReturnsAsync(150);
            var redeems = new Mock<IProductRedeemRepository>();

            var (ok, error) = await CreateService(products, wallets, redeems).PurchaseAsync(1, "u1", SampleAddress());

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(4, product.Stock);
            wallets.Verify(w => w.AddTransactionAsync(It.Is<CoinWalletTransactionDomain>(
                t => t.UserId == "u1" && t.Amount == 100 && t.Type == CoinWalletTransactionType.Spend)), Times.Once);
            redeems.Verify(r => r.AddAsync(It.Is<ProductRedeemDomain>(
                p => p.UserId == "u1" && p.ProductId == 1
                && p.ShippingFullName == "Jan Kowalski" && p.ShippingCity == "Warszawa")), Times.Once);
        }
    }
}
