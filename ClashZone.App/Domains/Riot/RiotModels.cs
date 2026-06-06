namespace ClashZone.App.Domains.Riot
{
    /// <summary>
    /// Konto Riot rozwiązane z Riot ID ("Nazwa#TAG") przez endpoint Account-V1.
    /// </summary>
    public sealed class RiotAccount
    {
        public string Puuid { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string TagLine { get; set; } = string.Empty;

        public string RiotId => $"{GameName}#{TagLine}";
    }

    /// <summary>
    /// Pojedynczy mecz League of Legends pobrany z Match-V5 (jedna gra = jedna mapa).
    /// Zawiera tylko pola potrzebne ClashZone do zbudowania wyniku i statystyk.
    /// </summary>
    public sealed class RiotMatch
    {
        public string MatchId { get; set; } = string.Empty;
        public List<RiotMatchParticipant> Participants { get; set; } = new();
    }

    /// <summary>
    /// Statystyki jednego uczestnika meczu LoL (z Match-V5).
    /// </summary>
    public sealed class RiotMatchParticipant
    {
        public string Puuid { get; set; } = string.Empty;

        /// <summary>Strona w grze: 100 (niebiescy) lub 200 (czerwoni).</summary>
        public int TeamId { get; set; }

        public bool Win { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string ChampionName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wynik meczu ClashZone odtworzony z prawdziwych danych Riota: punktacja obu
    /// drużyn (1-0 dla pojedynczej gry) oraz statystyki graczy zmapowane już na
    /// identyfikatory użytkowników ClashZone.
    /// </summary>
    public sealed class RiotMatchOutcome
    {
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
        public List<RiotMatchPlayerStat> Players { get; set; } = new();

        /// <summary>Identyfikator meczu Riota, z którego pochodzą dane (do śledzenia/idempotencji).</summary>
        public string SourceMatchId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statystyki gracza zmapowane na użytkownika ClashZone (po PUUID).
    /// </summary>
    public sealed class RiotMatchPlayerStat
    {
        public string UserId { get; set; } = string.Empty;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string? Champion { get; set; }
    }
}
