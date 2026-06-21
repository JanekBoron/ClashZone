using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace ClashZone.E2E.Tests.Infrastructure
{
    public abstract class E2ETestBase : PageTest
    {
        protected const string DefaultPassword = "Test#123";

        protected const string AdminEmail = "admin@clashzone.local";
        protected const string AdminPassword = "Admin#123";

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

        protected async Task RegisterAsync(string email, string userName, string password)
        {
            await Page.GotoAsync("/Account/Register");
            await Page.FillAsync("#Email", email);
            await Page.FillAsync("#UserName", userName);
            await Page.FillAsync("#Password", password);
            await Page.FillAsync("#ConfirmPassword", password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Zarejestruj" }).ClickAsync();
        }

        protected async Task LoginAsync(string email, string password)
        {
            await Page.GotoAsync("/Account/Login");
            await Page.FillAsync("#Email", email);
            await Page.FillAsync("#Password", password);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj" }).ClickAsync();
        }

        protected async Task LogoutAsync()
        {
            await Page.ClickAsync("#logoutForm button[type=submit]");
        }

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
