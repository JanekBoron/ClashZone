namespace ClashZone.App.Domains.Riot
{
    public sealed class RiotAccount
    {
        public string Puuid { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string TagLine { get; set; } = string.Empty;
        public string RiotId => $"{GameName}#{TagLine}";
    }

    public sealed class RiotMatch
    {
        public string MatchId { get; set; } = string.Empty;
        public List<RiotMatchParticipant> Participants { get; set; } = new();
    }

    public sealed class RiotMatchParticipant
    {
        public string Puuid { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public bool Win { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string ChampionName { get; set; } = string.Empty;
    }

    public sealed class RiotMatchOutcome
    {
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
        public List<RiotMatchPlayerStat> Players { get; set; } = new();
        public string SourceMatchId { get; set; } = string.Empty;
    }

    public sealed class RiotMatchPlayerStat
    {
        public string UserId { get; set; } = string.Empty;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string? Champion { get; set; }
    }
}
