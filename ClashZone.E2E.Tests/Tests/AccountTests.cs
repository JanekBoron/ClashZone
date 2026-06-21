using System.Text.RegularExpressions;
using ClashZone.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ClashZone.E2E.Tests.Tests
{
    [TestFixture]
    public class AccountTests : E2ETestBase
    {
        [Test]
        public async Task Register_then_login_shows_greeting()
        {
            var email = UniqueEmail();
            var userName = UniqueName();

            await RegisterAsync(email, userName, DefaultPassword);
            await LoginAsync(email, DefaultPassword);

            await Expect(Page).ToHaveURLAsync(new Regex(@"127\.0\.0\.1:5099/?$"));
            await Expect(Page.GetByText($"Cześć, {userName}!")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Login_with_invalid_credentials_shows_error()
        {
            await LoginAsync(UniqueEmail(), "WrongPass#1");

            await Expect(Page.GetByText("Nieprawidłowa próba logowania.")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Logout_returns_to_anonymous_state()
        {
            var email = UniqueEmail();
            var userName = UniqueName();
            await RegisterAsync(email, userName, DefaultPassword);
            await LoginAsync(email, DefaultPassword);
            await Expect(Page.GetByText($"Cześć, {userName}!")).ToBeVisibleAsync();

            await LogoutAsync();

            await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Logowanie" })).ToBeVisibleAsync();
        }

        [Test]
        public async Task Anonymous_user_is_redirected_to_login_on_protected_page()
        {
            await Page.GotoAsync("/Tournaments/MyTournaments");

            await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
        }
    }
}
