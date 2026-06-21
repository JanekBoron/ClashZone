namespace ClashZone.App.Services.Games.Riot
{
    public sealed class RiotOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string RegionalHost { get; set; } = "https://europe.api.riotgames.com";
        public int MatchLookbackCount { get; set; } = 5;
        public bool DevMode { get; set; }
        public string DevSeedRiotId { get; set; } = string.Empty;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
