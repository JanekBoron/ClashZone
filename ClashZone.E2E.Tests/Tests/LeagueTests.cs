using System.Text.RegularExpressions;
using ClashZone.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ClashZone.E2E.Tests.Tests
{
    [TestFixture]
    public class LeagueTests : E2ETestBase
    {
        private const string SeededSeason = "ClashZone League - Sezon 1";
        private const string PlanUltra = "Ultra";

        private async Task ActivateLeaguePackageAsync()
        {
            await Page.GotoAsync("/Subscription/Index");
            var ultraCard = Page.Locator(".card").Filter(new() { HasText = PlanUltra });
            var planId = await ultraCard.Locator("input[name=planId]").GetAttributeAsync("value");
            await Page.GotoAsync($"/Payment/Success?planId={planId}");
        }

        private async Task OpenSeededSeasonAsync()
        {
            await Page.GotoAsync("/League/Index");
            await Page.GetByRole(AriaRole.Link, new() { Name = "Zobacz sezon" }).First.ClickAsync();
        }

        [Test]
        public async Task League_index_is_public_and_lists_seeded_season()
        {
            await Page.GotoAsync("/League/Index");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Liga", Exact = true })).ToBeVisibleAsync();
            await Expect(Page.GetByText(SeededSeason)).ToBeVisibleAsync();
        }

        [Test]
        public async Task Anonymous_season_details_redirect_to_login()
        {
            await Page.GotoAsync("/League/Details/1");

            await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
        }

        [Test]
        public async Task Without_league_package_registration_is_blocked()
        {
            await RegisterAndLoginAsync();
            await OpenSeededSeasonAsync();

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Zgłoś drużynę" })).ToBeVisibleAsync();
            await Expect(Page.GetByText("wymaga pakietu")).ToBeVisibleAsync();
        }

        [Test]
        public async Task League_package_unlocks_registration_prompt()
        {
            await RegisterAndLoginAsync();
            await ActivateLeaguePackageAsync();

            await OpenSeededSeasonAsync();

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Zgłoś drużynę" })).ToBeVisibleAsync();
            await Expect(Page.GetByText("wymaga pakietu")).Not.ToBeVisibleAsync();
            await Expect(Page.GetByText("Brak kwalifikujących się drużyn")).ToBeVisibleAsync();
        }

        [Test]
        public async Task User_can_create_persistent_league_team()
        {
            await RegisterAndLoginAsync();
            await Page.GotoAsync("/Team/Create");

            var teamName = $"E2E Team {Guid.NewGuid():N}".Substring(0, 18);
            await Page.FillAsync("#Name", teamName);
            await Page.SelectOptionAsync("#Game", "Counter Strike 2");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Utwórz" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex("/Team/Details"));
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = teamName })).ToBeVisibleAsync();
            await Expect(Page.GetByText("Skład (1)")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Admin_can_open_league_management()
        {
            await LoginAsync(AdminEmail, AdminPassword);
            await Page.GotoAsync("/League/Manage");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Zarządzanie ligą" })).ToBeVisibleAsync();
            await Expect(Page.GetByText(SeededSeason)).ToBeVisibleAsync();
        }

        [Test]
        public async Task Normal_user_cannot_open_league_management()
        {
            await RegisterAndLoginAsync();
            await Page.GotoAsync("/League/Manage");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Zarządzanie ligą" })).Not.ToBeVisibleAsync();
        }
    }
}
