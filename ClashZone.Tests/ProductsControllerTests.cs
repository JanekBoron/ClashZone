using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.App.Domains;
using ClashZone.App.Services.Interfaces;
using ClashZone.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    public class ProductsControllerTests
    {
        private static ProductsController CreateController(Mock<IProductsService> service)
        {
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(e => e.WebRootPath).Returns(System.IO.Path.GetTempPath());
            return new ProductsController(service.Object, env.Object);
        }
        [Fact]
        public async Task Index_ReturnsViewWithProducts()
        {
            var service = new Mock<IProductsService>();
            var items = new List<ProductDomain> { new() { Id = 1, Name = "Hoodie" } };
            service.Setup(s => s.GetAllAsync()).ReturnsAsync(items);

            var result = await CreateController(service).Index();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(items, view.Model);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithoutSaving()
        {
            var service = new Mock<IProductsService>();
            var controller = CreateController(service);
            controller.ModelState.AddModelError("Name", "required");

            var model = new ProductDomain();
            var result = await controller.Create(model, null);

            Assert.IsType<ViewResult>(result);
            service.Verify(s => s.CreateAsync(It.IsAny<ProductDomain>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task Create_Post_Valid_CreatesAndRedirects()
        {
            var service = new Mock<IProductsService>();
            var controller = CreateController(service);
            var model = new ProductDomain { Name = "Cap" };

            var result = await controller.Create(model, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            service.Verify(s => s.CreateAsync(model, null), Times.Once);
        }

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenMissing()
        {
            var service = new Mock<IProductsService>();
            service.Setup(s => s.GetAsync(7)).ReturnsAsync((ProductDomain?)null);

            var result = await CreateController(service).Edit(7);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ConfirmDelete_DeletesAndRedirects()
        {
            var service = new Mock<IProductsService>();
            var controller = CreateController(service);

            var result = await controller.ConfirmDelete(3);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            service.Verify(s => s.DeleteAsync(3), Times.Once);
        }
    }
}
