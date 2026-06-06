namespace ClashZone.App.Services.Games.Riot
{
    /// <summary>
    /// Konfiguracja integracji z Riot Games API (sekcja "Riot" w konfiguracji).
    /// Klucz trzymamy poza repo (user-secrets / zmienne środowiskowe). Gdy klucz
    /// nie jest ustawiony, integracja jest wyłączona i nie blokuje działania aplikacji.
    /// </summary>
    public sealed class RiotOptions
    {
        /// <summary>Klucz developerski/produkcyjny Riota (nagłówek X-Riot-Token).</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Host regionalny dla Account-V1 i Match-V5: europe / americas / asia.
        /// Domyślnie europe (zgodnie z głównym regionem ClashZone).
        /// </summary>
        public string RegionalHost { get; set; } = "https://europe.api.riotgames.com";

        /// <summary>Ile ostatnich meczów gracza przejrzeć w poszukiwaniu meczu turniejowego.</summary>
        public int MatchLookbackCount { get; set; } = 5;

        /// <summary>
        /// Tryb deweloperski. Gdy włączony, wynik LoL budujemy z najnowszego
        /// realnego meczu jednego konta-źródła (admina), zamiast szukać wspólnej
        /// gry obu drużyn. Pozwala demonstrować feature bez rozgrywania meczu
        /// turniejowego przez oba składy. Custom games nadal nie zadziałają —
        /// Riot ich nie zwraca; konto musi mieć rozegraną grę z matchmakingu.
        /// </summary>
        public bool DevMode { get; set; }

        /// <summary>
        /// Riot ID admina ("Nazwa#TAG") używany jako konto-źródło w trybie
        /// deweloperskim. Gdy puste, używamy pierwszego powiązanego konta w meczu.
        /// </summary>
        public string DevSeedRiotId { get; set; } = string.Empty;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
