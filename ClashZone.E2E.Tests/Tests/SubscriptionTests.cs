using System.Text.RegularExpressions;
using ClashZone.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ClashZone.E2E.Tests.Tests
{
    /// <summary>
    /// Przepływy subskrypcji i płatności: ochrona strony pakietów, prezentacja
    /// seedowanych planów oraz powrót z bramki płatności (sukces / anulowanie).
    ///
    /// Powrotu ze Stripe nie da się (i nie ma potrzeby) wyklikać w zewnętrznej
    /// bramce – zamiast tego wchodzimy bezpośrednio na akcje Payment/Success oraz
    /// Payment/Cancel, czyli dokładnie te adresy, które aplikacja przekazuje
    /// Stripe jako SuccessUrl i CancelUrl. Dzięki temu test nie wychodzi do
    /// zewnętrznego serwisu, a i tak przechodzi przez prawdziwy PaymentController
    /// (utworzenie subskrypcji / komunikat o anulowaniu).
    /// </summary>
    [TestFixture]
    public class SubscriptionTests : E2ETestBase
    {
        // Plany seedowane przez DbInitializer.
        private const string PlanPremium = "Premium";
        private const string PlanLeague = "League";
        private const string PlanUltra = "Ultra";

        [Test]
        public async Task Anonymous_subscription_page_redirects_to_login()
        {
            // SubscriptionController jest [Authorize] na poziomie klasy.
            await Page.GotoAsync("/Subscription/Index");

            await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
        }

        [Test]
        public async Task Subscription_page_lists_seeded_plans()
        {
            await RegisterAndLoginAsync();
            await Page.GotoAsync("/Subscription/Index");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Pakiety subskrypcyjne" })).ToBeVisibleAsync();
            // Tytuły planów to nagłówki kart (h5).
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = PlanPremium })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = PlanLeague })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = PlanUltra })).ToBeVisibleAsync();
            // Każdy plan ma przycisk rozpoczynający zakup (kierujący do Stripe Checkout).
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Wykup" }).First).ToBeVisibleAsync();
        }

        [Test]
        public async Task Cancelled_payment_shows_warning_on_subscription_page()
        {
            await RegisterAndLoginAsync();

            // Stripe po anulowaniu przekierowuje na ten adres.
            await Page.GotoAsync("/Payment/Cancel");

            await Expect(Page).ToHaveURLAsync(new Regex("/Subscription"));
            await Expect(Page.GetByText("Transakcja została anulowana.")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Successful_payment_activates_subscription()
        {
            await RegisterAndLoginAsync();
            await Page.GotoAsync("/Subscription/Index");

            // Świeży użytkownik nie ma jeszcze aktywnego pakietu.
            await Expect(Page.GetByText("Twój aktywny pakiet:")).Not.ToBeVisibleAsync();

            // Odczytaj realne Id planu "Ultra" z formularza na stronie – odporne na
            // zmianę kolejności seedu (nie hardkodujemy identyfikatora).
            var ultraCard = Page.Locator(".card").Filter(new() { HasText = PlanUltra });
            var planId = await ultraCard.Locator("input[name=planId]").GetAttributeAsync("value");
            Assert.That(planId, Is.Not.Null.And.Not.Empty);

            // Stripe po udanej płatności przekierowuje dokładnie na ten adres.
            await Page.GotoAsync($"/Payment/Success?planId={planId}");

            await Expect(Page).ToHaveURLAsync(new Regex("/Subscription"));
            // Baner aktywnego pakietu pokazuje nazwę wykupionego planu.
            var activeBanner = Page.Locator(".alert-info");
            await Expect(activeBanner).ToContainTextAsync("Twój aktywny pakiet:");
            await Expect(activeBanner).ToContainTextAsync(PlanUltra);
        }
    }
}
