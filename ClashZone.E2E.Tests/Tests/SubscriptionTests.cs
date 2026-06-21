using System.Text.RegularExpressions;
using ClashZone.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ClashZone.E2E.Tests.Tests
{
    [TestFixture]
    public class SubscriptionTests : E2ETestBase
    {
        private const string PlanPremium = "Premium";
        private const string PlanLeague = "League";
        private const string PlanUltra = "Ultra";

        [Test]
        public async Task Anonymous_subscription_page_redirects_to_login()
        {
            await Page.GotoAsync("/Subscription/Index");

            await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
        }

        [Test]
        public async Task Subscription_page_lists_seeded_plans()
        {
            await RegisterAndLoginAsync();
            await Page.GotoAsync("/Subscription/Index");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Pakiety subskrypcyjne" })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = PlanPremium })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = PlanLeague })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = PlanUltra })).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Wykup" }).First).ToBeVisibleAsync();
        }

        [Test]
        public async Task Cancelled_payment_shows_warning_on_subscription_page()
        {
            await RegisterAndLoginAsync();

            await Page.GotoAsync("/Payment/Cancel");

            await Expect(Page).ToHaveURLAsync(new Regex("/Subscription"));
            await Expect(Page.GetByText("Transakcja została anulowana.")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Successful_payment_activates_subscription()
        {
            await RegisterAndLoginAsync();
            await Page.GotoAsync("/Subscription/Index");

            await Expect(Page.GetByText("Twój aktywny pakiet:")).Not.ToBeVisibleAsync();

            var ultraCard = Page.Locator(".card").Filter(new() { HasText = PlanUltra });
            var planId = await ultraCard.Locator("input[name=planId]").GetAttributeAsync("value");
            Assert.That(planId, Is.Not.Null.And.Not.Empty);

            await Page.GotoAsync($"/Payment/Success?planId={planId}");

            await Expect(Page).ToHaveURLAsync(new Regex("/Subscription"));
            var activeBanner = Page.Locator(".alert-info");
            await Expect(activeBanner).ToContainTextAsync("Twój aktywny pakiet:");
            await Expect(activeBanner).ToContainTextAsync(PlanUltra);
        }
    }
}
