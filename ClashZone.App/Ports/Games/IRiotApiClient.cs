using ClashZone.App.Domains.Riot;

namespace ClashZone.App.Ports.Games
{
    public interface IRiotApiClient
    {
        bool IsConfigured { get; }

        Task<RiotAccount?> ResolveAccountAsync(string gameName, string tagLine, CancellationToken ct = default);

        Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, int count, CancellationToken ct = default);

        Task<RiotMatch?> GetMatchAsync(string matchId, CancellationToken ct = default);
    }
}
