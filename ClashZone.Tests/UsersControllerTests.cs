using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using ClashZone.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="UsersController"/> (panel admina) — listowanie oraz
    /// operacje na rolach i banach.
    /// </summary>
    public class UsersControllerTests
    {
        [Fact]
        public async Task Index_ReturnsViewWithUsers()
        {
            var svc = new Mock<IUserAdminService>();
            var users = new List<UserListItemVm> { new() { Id = "1", UserName = "alpha" } };
            svc.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

            var result = await new UsersController(svc.Object).Index();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(users, view.Model);
        }

        [Fact]
        public async Task MakeAdmin_AddsRole_AndRedirects()
        {
            var svc = new Mock<IUserAdminService>();
            var controller = new UsersController(svc.Object);

            var result = await controller.MakeAdmin("u1");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            svc.Verify(s => s.AddToRoleAsync("u1", "Admin"), Times.Once);
        }

        [Fact]
        public async Task Ban_BansUser_AndRedirects()
        {
            var svc = new Mock<IUserAdminService>();
            var controller = new UsersController(svc.Object);

            var result = await controller.Ban("u1");

            Assert.IsType<RedirectToActionResult>(result);
            svc.Verify(s => s.BanAsync("u1"), Times.Once);
        }

        [Fact]
        public async Task Unban_UnbansUser_AndRedirects()
        {
            var svc = new Mock<IUserAdminService>();
            var controller = new UsersController(svc.Object);

            var result = await controller.Unban("u1");

            Assert.IsType<RedirectToActionResult>(result);
            svc.Verify(s => s.UnbanAsync("u1"), Times.Once);
        }
    }
}
