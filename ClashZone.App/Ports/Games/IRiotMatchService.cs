using ClashZone.App.Domains.Riot;

namespace ClashZone.App.Ports.Games
{
    public interface IRiotMatchService
    {
        Task<RiotMatchOutcome?> ResolveMatchAsync(
            IReadOnlyList<string> team1UserIds,
            IReadOnlyList<string> team2UserIds,
            bool forceDevMode = false,
            CancellationToken ct = default);
    }
}
