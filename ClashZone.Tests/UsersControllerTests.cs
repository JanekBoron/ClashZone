using ClashZone.Services.Interfaces;
using ClashZone.Web.Controllers;
using ClashZone.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Tests for the administrative <see cref="UsersController"/>.  Each action
    /// should delegate to the appropriate method on the <see cref="IUserAdminService"/>
    /// and redirect back to the index.  The index action returns a view with
    /// the list of users.
    /// </summary>
    public class UsersControllerTests
    {
        private static UsersController CreateController(IUserAdminService svc)
        {
            return new UsersController(svc);
        }

        [Fact]
        public async Task Index_ReturnsViewWithUsers()
        {
            // Arrange
            var list = new List<UserListItemVm>
    {
        new UserListItemVm(),  
        new UserListItemVm()
    };

            var svcMock = new Mock<IUserAdminService>();

            svcMock.Setup(s => s.GetAllAsync())
                   .ReturnsAsync(list);


            var controller = CreateController(svcMock.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<UserListItemVm>>(view.Model);

            Assert.Same(list, model);
        }
        [Fact]
        public async Task MakeOrganizer_CallsServiceAndRedirects()
        {
            var svcMock = new Mock<IUserAdminService>();
            var controller = CreateController(svcMock.Object);
            var result = await controller.MakeOrganizer("id");
            svcMock.Verify(s => s.AddToRoleAsync("id", "Organizer"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(UsersController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task MakeAdmin_CallsServiceAndRedirects()
        {
            var svcMock = new Mock<IUserAdminService>();
            var controller = CreateController(svcMock.Object);
            var result = await controller.MakeAdmin("id");
            svcMock.Verify(s => s.AddToRoleAsync("id", "Admin"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(UsersController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task RemoveOrganizer_CallsServiceAndRedirects()
        {
            var svcMock = new Mock<IUserAdminService>();
            var controller = CreateController(svcMock.Object);
            var result = await controller.RemoveOrganizer("id");
            svcMock.Verify(s => s.RemoveFromRoleAsync("id", "Organizer"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(UsersController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task RemoveAdmin_CallsServiceAndRedirects()
        {
            var svcMock = new Mock<IUserAdminService>();
            var controller = CreateController(svcMock.Object);
            var result = await controller.RemoveAdmin("id");
            svcMock.Verify(s => s.RemoveFromRoleAsync("id", "Admin"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(UsersController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task Ban_CallsServiceAndRedirects()
        {
            var svcMock = new Mock<IUserAdminService>();
            var controller = CreateController(svcMock.Object);
            var result = await controller.Ban("id");
            svcMock.Verify(s => s.BanAsync("id"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(UsersController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task Unban_CallsServiceAndRedirects()
        {
            var svcMock = new Mock<IUserAdminService>();
            var controller = CreateController(svcMock.Object);
            var result = await controller.Unban("id");
            svcMock.Verify(s => s.UnbanAsync("id"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(UsersController.Index), redirect.ActionName);
        }
    }
}