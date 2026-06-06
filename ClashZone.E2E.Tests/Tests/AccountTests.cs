using System.Text.RegularExpressions;
using ClashZone.E2E.Tests.Infrastructure;
using Microsoft.Playwright;
using NUnit.Framework;

namespace ClashZone.E2E.Tests.Tests
{
    /// <summary>
    /// Przepływy konta użytkownika: rejestracja, logowanie, wylogowanie oraz
    /// ochrona stron wymagających zalogowania.
    /// </summary>
    [TestFixture]
    public class AccountTests : E2ETestBase
    {
        [Test]
        public async Task Register_then_login_shows_greeting()
        {
            var email = UniqueEmail();
            var userName = UniqueName();

            // Rejestracja nie loguje automatycznie – po niej logujemy się jawnie.
            await RegisterAsync(email, userName, DefaultPassword);
            await LoginAsync(email, DefaultPassword);

            // Po udanym logowaniu wracamy na stronę główną (redirect do Home/Index = "/").
            await Expect(Page).ToHaveURLAsync(new Regex(@"127\.0\.0\.1:5099/?$"));
            await Expect(Page.GetByText($"Hello, {userName}!")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Login_with_invalid_credentials_shows_error()
        {
            await LoginAsync(UniqueEmail(), "WrongPass#1");

            // Komunikat jest celowo ogólny (nie zdradza, czy e-mail istnieje).
            await Expect(Page.GetByText("Nieprawidłowa próba logowania.")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Logout_returns_to_anonymous_state()
        {
            var email = UniqueEmail();
            var userName = UniqueName();
            await RegisterAsync(email, userName, DefaultPassword);
            await LoginAsync(email, DefaultPassword);
            await Expect(Page.GetByText($"Hello, {userName}!")).ToBeVisibleAsync();

            await LogoutAsync();

            // Po wylogowaniu w navbarze ponownie pojawiają się linki logowania/rejestracji.
            await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Logowanie" })).ToBeVisibleAsync();
        }

        [Test]
        public async Task Anonymous_user_is_redirected_to_login_on_protected_page()
        {
            // MyTournaments wymaga [Authorize]; anonim powinien trafić na logowanie.
            await Page.GotoAsync("/Tournaments/MyTournaments");

            await Expect(Page).ToHaveURLAsync(new Regex("/Account/Login"));
        }
    }
}
