using System.Threading.Tasks;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using ClashZone.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="BracketController"/> — symulacja meczu zwraca widok
    /// drabinki lub przekierowanie, gdy symulacja jest niemożliwa.
    /// </summary>
    public class BracketControllerTests
    {
        [Fact]
        public async Task SimulateMatch_RedirectsToBracket_WhenServiceReturnsNull()
        {
            var service = new Mock<IBracketService>();
            service.Setup(s => s.SimulateMatchAsync(1, 1, 1, It.IsAny<bool>())).ReturnsAsync((BracketViewModel?)null);

            var result = await new BracketController(service.Object).SimulateMatch(1, 1, 1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bracket", redirect.ActionName);
            Assert.Equal("Tournaments", redirect.ControllerName);
        }

        [Fact]
        public async Task SimulateMatch_ReturnsBracketView_WhenSimulated()
        {
            var vm = new BracketViewModel();
            var service = new Mock<IBracketService>();
            service.Setup(s => s.SimulateMatchAsync(1, 1, 1, It.IsAny<bool>())).ReturnsAsync(vm);

            var result = await new BracketController(service.Object).SimulateMatch(1, 1, 1);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(vm, view.Model);
        }
    }
}
