using ClashZone.App.Domains.Riot;

namespace ClashZone.App.Ports.Games
{
    /// <summary>
    /// Cienka abstrakcja nad publicznym Riot Games API (Account-V1 + Match-V5).
    /// Implementacja używa typed <see cref="System.Net.Http.HttpClient"/>; dzięki
    /// portowi serwisy domenowe i testy nie zależą bezpośrednio od HTTP.
    /// Wszystkie metody zwracają null/pustą listę zamiast rzucać przy błędach sieci
    /// lub gdy integracja nie jest skonfigurowana.
    /// </summary>
    public interface IRiotApiClient
    {
        /// <summary>Czy ustawiono klucz API (gdy nie — integracja jest wyłączona).</summary>
        bool IsConfigured { get; }

        /// <summary>Rozwiązuje Riot ID ("Nazwa#TAG") na konto z PUUID (Account-V1).</summary>
        Task<RiotAccount?> ResolveAccountAsync(string gameName, string tagLine, CancellationToken ct = default);

        /// <summary>Pobiera identyfikatory ostatnich meczów gracza (Match-V5).</summary>
        Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, int count, CancellationToken ct = default);

        /// <summary>Pobiera szczegóły meczu wraz ze statystykami uczestników (Match-V5).</summary>
        Task<RiotMatch?> GetMatchAsync(string matchId, CancellationToken ct = default);
    }
}
