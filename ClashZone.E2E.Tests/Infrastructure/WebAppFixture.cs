using System.Diagnostics;
using NUnit.Framework;

namespace ClashZone.E2E.Tests
{
    [SetUpFixture]
    public class WebAppFixture
    {
        public const string BaseUrl = "http://127.0.0.1:5099";

        private static string TestConnectionString =>
            Environment.GetEnvironmentVariable("CLASHZONE_E2E_CONNECTION")
            ?? "Server=localhost\\SQLEXPRESS;Database=CLASHZONE_E2E;User Id=sa;Password=123;TrustServerCertificate=true";

        private Process? _appProcess;
        private bool _startedByUs;

        [OneTimeSetUp]
        public async Task StartAppAsync()
        {
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
                }
                await Task.Delay(1000);
            }
            return false;
        }

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
