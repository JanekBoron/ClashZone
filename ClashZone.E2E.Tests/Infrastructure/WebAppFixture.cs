using System.Diagnostics;
using NUnit.Framework;

// UWAGA: namespace celowo nadrzędny (ClashZone.E2E.Tests), aby [SetUpFixture] objął
// wszystkie testy w pod-namespace'ach (np. ClashZone.E2E.Tests.Tests). SetUpFixture
// działa tylko dla swojego namespace i jego potomków.
namespace ClashZone.E2E.Tests
{
    /// <summary>
    /// Uruchamia prawdziwą aplikację ClashZone (Kestrel) raz na całą sesję testów E2E
    /// i zatrzymuje ją po zakończeniu. Aplikacja celuje w osobną bazę
    /// <c>CLASHZONE_E2E</c>, dzięki czemu testy nigdy nie dotykają danych
    /// deweloperskich ani produkcyjnych. Migracje i seed (admin, przykładowe turnieje,
    /// plany subskrypcji) wykonuje sama aplikacja w <c>DbInitializer</c> na starcie.
    /// </summary>
    [SetUpFixture]
    public class WebAppFixture
    {
        /// <summary>Adres, pod którym wystawiamy aplikację dla przeglądarki Playwright.</summary>
        public const string BaseUrl = "http://127.0.0.1:5099";

        /// <summary>
        /// Połączenie do osobnej bazy testowej. Pochodzenie z appsettings.json, ale inna
        /// nazwa bazy. Można nadpisać zmienną środowiskową CLASHZONE_E2E_CONNECTION.
        /// </summary>
        private static string TestConnectionString =>
            Environment.GetEnvironmentVariable("CLASHZONE_E2E_CONNECTION")
            ?? "Server=localhost\\SQLEXPRESS;Database=CLASHZONE_E2E;User Id=sa;Password=123;TrustServerCertificate=true";

        private Process? _appProcess;
        private bool _startedByUs;

        [OneTimeSetUp]
        public async Task StartAppAsync()
        {
            // Jeśli instancja już odpowiada (np. uruchomiona ręcznie pod tym portem), użyj jej.
            if (await IsUpAsync(TimeSpan.FromSeconds(2)))
            {
                TestContext.Progress.WriteLine($"[fixture] Reusing app already listening at {BaseUrl}");
                return;
            }

            var webCsproj = LocateWebCsproj();
            TestContext.Progress.WriteLine($"[fixture] Starting app from {webCsproj}");

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{webCsproj}\" --no-launch-profile -c Debug",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(webCsproj)!
            };
            // Tylko HTTP -> UseHttpsRedirection nie ma dokąd przekierować i serwuje po HTTP.
            psi.Environment["ASPNETCORE_URLS"] = BaseUrl;
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            psi.Environment["ConnectionStrings__DefaultConnection"] = TestConnectionString;

            _appProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _appProcess.OutputDataReceived += (_, e) => { if (e.Data != null) TestContext.Progress.WriteLine($"[app] {e.Data}"); };
            _appProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) TestContext.Progress.WriteLine($"[app:err] {e.Data}"); };
            _appProcess.Start();
            _appProcess.BeginOutputReadLine();
            _appProcess.BeginErrorReadLine();
            _startedByUs = true;

            // Pierwszy start robi migracje + seed, więc dajemy spory zapas czasu.
            if (!await IsUpAsync(TimeSpan.FromSeconds(120)))
            {
                throw new InvalidOperationException(
                    $"Aplikacja nie wystartowała pod {BaseUrl} w wyznaczonym czasie. " +
                    "Sprawdź czy działa SQL Server (localhost\\SQLEXPRESS) i czy połączenie jest poprawne.");
            }

            TestContext.Progress.WriteLine($"[fixture] App is up at {BaseUrl}");
        }

        [OneTimeTearDown]
        public void StopApp()
        {
            if (!_startedByUs || _appProcess == null)
                return;

            try
            {
                if (!_appProcess.HasExited)
                {
                    // dotnet run odpala aplikację jako proces potomny – ubijamy całe drzewo.
                    _appProcess.Kill(entireProcessTree: true);
                    _appProcess.WaitForExit(10_000);
                }
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"[fixture] Failed to stop app: {ex.Message}");
            }
            finally
            {
                _appProcess.Dispose();
            }
        }

        private static async Task<bool> IsUpAsync(TimeSpan timeout)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var res = await client.GetAsync(BaseUrl + "/");
                    if ((int)res.StatusCode < 500)
                        return true;
                }
                catch
                {
                    // serwer jeszcze nie słucha – próbujemy dalej
                }
                await Task.Delay(1000);
            }
            return false;
        }

        /// <summary>
        /// Idzie w górę od katalogu z testowym assembly aż napotka katalog repo,
        /// w którym leży projekt webowy (ClashZone\ClashZone.csproj). Dzięki temu
        /// nie zależymy od względnych ścieżek bin/.
        /// </summary>
        private static string LocateWebCsproj()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "ClashZone", "ClashZone.csproj");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            throw new InvalidOperationException(
                "Nie udało się znaleźć ClashZone\\ClashZone.csproj startując od " + AppContext.BaseDirectory);
        }
    }
}
