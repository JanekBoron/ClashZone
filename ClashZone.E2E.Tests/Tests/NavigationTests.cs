using ClashZone.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ClashZone.E2E.Tests.Tests
{
    [TestFixture]
    public class NavigationTests : E2ETestBase
    {
        [Test]
        public async Task Home_page_loads_with_welcome_heading()
        {
            await Page.GotoAsync("/");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Witaj w ClashZone!" }))
                .ToBeVisibleAsync();
        }

        [Test]
        public async Task Anonymous_navbar_shows_auth_links()
        {
            await Page.GotoAsync("/");

            await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Logowanie" })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Rejestracja" })).ToBeVisibleAsync();
        }

        [Test]
        public async Task Tournaments_link_navigates_to_list()
        {
            await Page.GotoAsync("/");

            await Page.ClickAsync("a[href='/Tournaments/Index']");

            await Expect(Page.GetByText(SeededCs2Tournament)).ToBeVisibleAsync();
        }
    }
}
