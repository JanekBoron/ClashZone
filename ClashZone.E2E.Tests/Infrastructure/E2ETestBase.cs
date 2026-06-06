using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace ClashZone.E2E.Tests.Infrastructure
{
    /// <summary>
    /// Wspólna baza dla testów E2E. Konfiguruje kontekst przeglądarki (bazowy adres,
    /// rozmiar okna) oraz dostarcza pomocnicze przepływy: rejestracja, logowanie,
    /// generowanie unikalnych danych i znane konto administratora z seedu.
    /// </summary>
    public abstract class E2ETestBase : PageTest
    {
        // Domyślne reguły hasła w ASP.NET Identity: min. 6 znaków, wielka i mała litera,
        // cyfra oraz znak specjalny.
        protected const string DefaultPassword = "Test#123";

        // Konto administratora seedowane przez RoleSeeder.
        protected const string AdminEmail = "admin@clashzone.local";
        protected const string AdminPassword = "Admin#123";

        // Turnieje seedowane przez DbInitializer.
        protected const string SeededCs2Tournament = "Summer CS2 Cup";
        protected const string SeededLolTournament = "LoL 1v1 Duel";

        public override BrowserNewContextOptions ContextOptions() => new()
        {
            BaseURL = WebAppFixture.BaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
        };

        protected static string UniqueEmail(string prefix = "e2e") =>
            $"{prefix}_{Guid.NewGuid():N}@clashzone.test";

        protected static string UniqueName(string prefix = "user") =>
            $"{prefix}_{Guid.NewGuid():N}".Substring(0, 16);

        /// <summary>Wypełnia i wysyła formularz rejestracji.</summary>
        protected async Task RegisterAsync(string email, string userName, string password)
        {
            await Page.GotoAsync("/Account/Register");
            await Page.FillAsync("#Email", email);
            await Page.FillAsync("#UserName", userName);
            await Page.FillAsync("#Password", password);
            await Page.FillAsync("#ConfirmPassword", password);
            // Celujemy w przycisk po nazwie – na stronach z zalogowanym userem
            // w navbarze jest dodatkowy submit ("Log off").
            await Page.GetByRole(AriaRole.Button, new() { Name = "Zarejestruj" }).ClickAsync();
        }

        /// <summary>Wypełnia i wysyła formularz logowania.</summary>
        protected async Task LoginAsync(string email, string password)
        {
            await Page.GotoAsync("/Account/Login");
            await Page.FillAsync("#Email", email);
            await Page.FillAsync("#Password", password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj" }).ClickAsync();
        }

        /// <summary>Wylogowuje przez przycisk "Log off" z paska nawigacji.</summary>
        protected async Task LogoutAsync()
        {
            await Page.ClickAsync("#logoutForm button[type=submit]");
        }

        /// <summary>
        /// Rejestruje świeże konto i od razu się na nie loguje. Zwraca nazwę
        /// użytkownika, która może być przydatna w asercjach (np. powitanie).
        /// </summary>
        protected async Task<string> RegisterAndLoginAsync(string prefix = "e2e")
        {
            var email = UniqueEmail(prefix);
            var userName = UniqueName();
            await RegisterAsync(email, userName, DefaultPassword);
            await LoginAsync(email, DefaultPassword);
            return userName;
        }
    }
}
