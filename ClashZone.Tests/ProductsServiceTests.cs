using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class ProductsServiceTests
    {
        [Fact]
        public async Task CreateAsync_AddsPrimaryImage_AndPersists()
        {
            var repo = new Mock<IProductsRepository>();
            var service = new ProductsService(repo.Object);
            var product = new ProductDomain { Name = "Hoodie" };

            await service.CreateAsync(product, "/img/hoodie.png");

            var img = Assert.Single(product.Images);
            Assert.True(img.IsPrimary);
            Assert.Equal("/img/hoodie.png", img.Url);
            repo.Verify(r => r.AddAsync(product), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Throws_WhenProductMissing()
        {
            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetAsync(5)).ReturnsAsync((ProductDomain?)null);
            var service = new ProductsService(repo.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.UpdateAsync(new ProductDomain { Id = 5 }, null));
        }

        [Fact]
        public async Task UpdateAsync_CopiesFields_AndPersists()
        {
            var existing = new ProductDomain { Id = 5, Name = "Old", ClashCoins = 10, IsActive = false };
            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetAsync(5)).ReturnsAsync(existing);
            var service = new ProductsService(repo.Object);

            await service.UpdateAsync(new ProductDomain { Id = 5, Name = "New", ClashCoins = 99, IsActive = true }, null);

            Assert.Equal("New", existing.Name);
            Assert.Equal(99, existing.ClashCoins);
            Assert.True(existing.IsActive);
            repo.Verify(r => r.UpdateAsync(existing), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DoesNothing_WhenMissing()
        {
            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetAsync(7)).ReturnsAsync((ProductDomain?)null);
            var service = new ProductsService(repo.Object);

            await service.DeleteAsync(7);

            repo.Verify(r => r.DeleteAsync(It.IsAny<ProductDomain>()), Times.Never);
        }
    }
}
