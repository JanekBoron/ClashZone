using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.Controllers;
using ClashZone.DataAccess.Models;
using ClashZone.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Tests for <see cref="ProductsController"/> which manages CRUD
    /// operations for products.  These tests verify that the controller
    /// correctly delegates to the underlying service, returns appropriate
    /// results when entities are missing and redirects after successful
    /// operations.
    /// </summary>
    public class ProductsControllerTests
    {
        private static ProductsController CreateController(IProductsService svc)
        {
            return new ProductsController(svc);
        }

        [Fact]
        public async Task Index_ReturnsViewWithItems()
        {
            // Arrange
            var items = new List<Product> { new Product { Id = 1 }, new Product { Id = 2 } };
            var svcMock = new Mock<IProductsService>();
            svcMock.Setup(s => s.GetAllAsync()).ReturnsAsync(items);
            var controller = CreateController(svcMock.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(items, view.Model);
        }

        [Fact]
        public void Create_Get_ReturnsViewWithDefaultProduct()
        {
            // Arrange
            var controller = CreateController(Mock.Of<IProductsService>());

            // Act
            var result = controller.Create();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Product>(view.Model);
            Assert.True(model.IsActive);
            Assert.Equal(100, model.ClashCoins);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var svcMock = new Mock<IProductsService>();
            var controller = CreateController(svcMock.Object);
            controller.ModelState.AddModelError("Name", "required");
            var model = new Product { Id = 0, Name = "Test" };

            // Act
            var result = await controller.Create(model, null);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, view.Model);
            svcMock.Verify(s => s.CreateAsync(It.IsAny<Product>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Create_Post_Valid_CallsServiceAndRedirects()
        {
            // Arrange
            var svcMock = new Mock<IProductsService>();
            var controller = CreateController(svcMock.Object);
            var model = new Product { Id = 0, Name = "Test" };

            // Act
            var result = await controller.Create(model, "img");

            // Assert
            svcMock.Verify(s => s.CreateAsync(model, "img"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenProductMissing()
        {
            // Arrange
            var svcMock = new Mock<IProductsService>();
            svcMock.Setup(s => s.GetAsync(5)).ReturnsAsync((Product?)null);
            var controller = CreateController(svcMock.Object);

            // Act
            var result = await controller.Edit(5);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WhenProductExists()
        {
            // Arrange
            var p = new Product { Id = 1, Name = "Prod" };
            var svcMock = new Mock<IProductsService>();
            svcMock.Setup(s => s.GetAsync(1)).ReturnsAsync(p);
            var controller = CreateController(svcMock.Object);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(p, view.Model);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var svcMock = new Mock<IProductsService>();
            var controller = CreateController(svcMock.Object);
            controller.ModelState.AddModelError("Name", "required");
            var model = new Product { Id = 0, Name = "Prod" };

            // Act
            var result = await controller.Edit(1, model, null);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, view.Model);
            svcMock.Verify(s => s.UpdateAsync(It.IsAny<Product>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Post_Valid_CallsUpdateAndRedirects()
        {
            // Arrange
            var svcMock = new Mock<IProductsService>();
            var controller = CreateController(svcMock.Object);
            var model = new Product { Name = "Prod" };

            // Act
            var result = await controller.Edit(10, model, "img");

            // Assert
            svcMock.Verify(s => s.UpdateAsync(It.Is<Product>(m => m.Id == 10), "img"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task Delete_Get_ReturnsNotFound_WhenProductMissing()
        {
            // Arrange
            var svcMock = new Mock<IProductsService>();
            svcMock.Setup(s => s.GetAsync(4)).ReturnsAsync((Product?)null);
            var controller = CreateController(svcMock.Object);

            // Act
            var result = await controller.Delete(4);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ReturnsView_WhenProductExists()
        {
            // Arrange
            var p = new Product { Id = 2 };
            var svcMock = new Mock<IProductsService>();
            svcMock.Setup(s => s.GetAsync(2)).ReturnsAsync(p);
            var controller = CreateController(svcMock.Object);

            // Act
            var result = await controller.Delete(2);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(p, view.Model);
        }

        [Fact]
        public async Task ConfirmDelete_Post_CallsDeleteAndRedirects()
        {
            // Arrange
            var svcMock = new Mock<IProductsService>();
            var controller = CreateController(svcMock.Object);

            // Act
            var result = await controller.ConfirmDelete(7);

            // Assert
            svcMock.Verify(s => s.DeleteAsync(7), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        }
    }
}