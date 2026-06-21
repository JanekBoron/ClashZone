

using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface IMatchesRepository
    {
        Task<List<MatchDomain>> GetMatchesByTournamentAsync(int tournamentId);
        Task<MatchDomain?> GetMatchByIdAsync(int matchId, int tournamentId);
        Task<List<PlayerMatchStatDomain>> GetPlayerStatsForMatchAsync(int matchId);
        Task<TeamDomain?> GetTeamByIdAsync(int teamId);
        Task AddMatchAsync(MatchDomain matchEntity);
        Task<bool> IsTournamentAlreadyAwardedAsync(int tournamentId, List<List<MatchInfoDomain>> rounds);
        Task<List<MatchDomain>> GetMatchesByTournamentIdAsync(int tournamentId);
        Task<bool> TournamentHasMatchesAsync(int tournamentId);

    }
}