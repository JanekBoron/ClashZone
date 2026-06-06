using ClashZone.App.Domains.Riot;

namespace ClashZone.App.Ports.Games
{
    /// <summary>
    /// Odpowiada za odtworzenie wyniku meczu ClashZone z prawdziwych danych Riota.
    /// Na kluczu developerskim (bez Tournament API) działa w trybie "match lookup":
    /// po powiązanych kontach graczy znajduje ostatnią wspólną grę obu drużyn
    /// w Match-V5 i mapuje ją na statystyki ClashZone.
    /// </summary>
    public interface IRiotMatchService
    {
        /// <summary>
        /// Próbuje odnaleźć i zmapować rozegrany mecz League of Legends dla pary drużyn.
        /// Zwraca null, gdy nie da się go ustalić (brak powiązanych kont, brak wspólnej
        /// gry, wyłączona integracja). Null oznacza "jeszcze brak danych" — nic nie zapisujemy.
        /// </summary>
        /// <param name="team1UserIds">Identyfikatory użytkowników ClashZone z drużyny 1.</param>
        /// <param name="team2UserIds">Identyfikatory użytkowników ClashZone z drużyny 2.</param>
        /// <param name="forceDevMode">
        /// Wymusza ścieżkę deweloperską (wynik z najnowszego meczu konta-źródła)
        /// niezależnie od flagi <c>Riot:DevMode</c>. Używane, gdy symulację odpala admin.
        /// </param>
        Task<RiotMatchOutcome?> ResolveMatchAsync(
            IReadOnlyList<string> team1UserIds,
            IReadOnlyList<string> team2UserIds,
            bool forceDevMode = false,
            CancellationToken ct = default);
    }
}
