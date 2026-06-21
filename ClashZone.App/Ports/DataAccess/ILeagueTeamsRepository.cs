using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{

    public interface ILeagueTeamsRepository
    {
        Task<LeagueTeamDomain> CreateTeamAsync(LeagueTeamDomain team);
        Task<LeagueTeamDomain?> GetByIdAsync(int id);
        Task<List<LeagueTeamDomain>> GetUserTeamsAsync(string userId);
        Task<List<string>> GetMemberIdsAsync(int teamId);
        Task AddMemberAsync(int teamId, string userId);
        Task<bool> IsMemberAsync(int teamId, string userId);
        Task<int> CountMembersJoinedSinceAsync(int teamId, DateTime since);
        Task<bool> UserJoinedAnyTeamSinceAsync(string userId, DateTime since);
    }
}
