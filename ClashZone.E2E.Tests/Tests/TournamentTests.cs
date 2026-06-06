using System.Text.RegularExpressions;
using ClashZone.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ClashZone.E2E.Tests.Tests
{
    /// <summary>
    /// Przepływy turniejów: przeglądanie listy (publiczne), filtrowanie po formacie,
    /// ochrona szczegółów dla anonima oraz tworzenie turnieju przez administratora.
    /// </summary>
    [TestFixture]
    public class TournamentTests : E2ETestBase
    {
        [Test]
        public async Task Browse_shows_seeded_tournaments()
        {
            await Page.GotoAsync("/Tournaments/Index");

            await Expect(Page.GetByText(SeededCs2Tournament)).ToBeVisibleAsync();
            await Expect(Page.GetByText(SeededLolTournament)).ToBeVisibleAsync();
        }

        [Test]
        public async Task Filter_by_1v1_hides_5v5_tournament()
        {
            // Seedowany "Summer CS2 Cup" jest 5v5, a "LoL 1v1 Duel" jest 1v1.
            await Page.GotoAsync("/Tournaments/Index?format=1v1");

            await Expect(Page.GetByText(SeededLolTournament)).ToBeVisibleAsync();
            await Expect(Page.GetByText(SeededCs2Tournament)).Not.ToBeVisibleAsync();
        }

        [Test]
        public async Task Anonymous_user_is_redirected_to_login_on_details()
        {
            // Szczegóły turnieju wymagają [Authorize].
            await Page.GotoAsync("/Tournaments/Details/1");

            await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
        }

        [Test]
        public async Task Admin_can_create_tournament()
        {
            await LoginAsync(AdminEmail, AdminPassword);

            await Page.GotoAsync("/Tournaments/Create");

            var name = $"E2E Cup {Guid.NewGuid():N}".Substring(0, 18);
            var startDate = DateTime.Now.AddMonths(1).ToString("yyyy-MM-ddTHH:mm");

            await Page.FillAsync("#Name", name);
            await Page.SelectOptionAsync("#gameTitleSelect", "Counter Strike 2");
            await Page.FillAsync("#StartDate", startDate);
            await Page.FillAsync("#MaxParticipants", "8");
            // CS2 dopuszcza 1v1, 2v2 i 5v5.
            await Page.SelectOptionAsync("#formatSelect", "1v1");
            await Page.FillAsync("#rulesTextarea", "Reguła testowa numer jeden.");

            // "Opublikuj" zamiast button[type=submit] – navbar ma drugi submit ("Log off").
            await Page.GetByRole(AriaRole.Button, new() { Name = "Opublikuj" }).ClickAsync();

            // Po utworzeniu wracamy na listę turniejów, gdzie widać nowy wpis.
            await Expect(Page).ToHaveURLAsync(new Regex("/Tournaments"));
            await Expect(Page.GetByText(name)).ToBeVisibleAsync();
        }
    }
}
