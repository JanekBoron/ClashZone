using System.Threading.Tasks;
using ClashZone.Controllers;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="BracketController"/>.  The controller
    /// delegates almost all work to the <see cref="IBracketService"/> and
    /// selects between returning a view or redirecting based on whether a
    /// bracket model is returned.  These tests verify that logic.
    /// </summary>
    public class BracketControllerTests
    {
        [Fact]
        public async Task SimulateMatch_RedirectsToBracket_WhenServiceReturnsNull()
        {
            // Arrange
            var svcMock = new Mock<IBracketService>();
            svcMock.Setup(s => s.SimulateMatchAsync(1, 2, 3))
                .ReturnsAsync((BracketViewModel?)null);
            var controller = new BracketController(svcMock.Object);

            // Act
            var result = await controller.SimulateMatch(1, 2, 3);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bracket", redirect.ActionName);
            Assert.Equal("Tournaments", redirect.ControllerName);
            Assert.True(redirect.RouteValues!.ContainsKey("id"));
            Assert.Equal(1, redirect.RouteValues!["id"]);
        }

        [Fact]
        public async Task SimulateMatch_ReturnsView_WhenServiceReturnsModel()
        {
            // Arrange
            var model = new BracketViewModel();
            var svcMock = new Mock<IBracketService>();
            svcMock.Setup(s => s.SimulateMatchAsync(5, 1, 1))
                .ReturnsAsync(model);
            var controller = new BracketController(svcMock.Object);

            // Act
            var result = await controller.SimulateMatch(5, 1, 1);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Tournaments/Bracket.cshtml", view.ViewName);
            Assert.Equal(model, view.Model);
        }
    }
}