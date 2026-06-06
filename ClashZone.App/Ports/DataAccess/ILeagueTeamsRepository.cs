using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    /// <summary>
    /// Dostęp do trwałych drużyn ligowych (Team Management) — niezależnych od
    /// sezonu. Hermetyzuje operacje EF dotyczące tworzenia drużyn, rosterów oraz
    /// zapytań potrzebnych do egzekwowania okresów karencji (cooldown).
    /// </summary>
    public interface ILeagueTeamsRepository
    {
        Task<LeagueTeamDomain> CreateTeamAsync(LeagueTeamDomain team);

        Task<LeagueTeamDomain?> GetByIdAsync(int id);

        /// <summary>Drużyny, w których użytkownik jest kapitanem lub członkiem.</summary>
        Task<List<LeagueTeamDomain>> GetUserTeamsAsync(string userId);

        Task<List<string>> GetMemberIdsAsync(int teamId);

        Task AddMemberAsync(int teamId, string userId);

        Task<bool> IsMemberAsync(int teamId, string userId);

        /// <summary>Liczba członków, którzy dołączyli do drużyny od podanej daty (cooldown 3/7 dni).</summary>
        Task<int> CountMembersJoinedSinceAsync(int teamId, DateTime since);

        /// <summary>Czy użytkownik dołączył do jakiejkolwiek drużyny od podanej daty (cooldown 1 zespół/7 dni).</summary>
        Task<bool> UserJoinedAnyTeamSinceAsync(string userId, DateTime since);
    }
}
