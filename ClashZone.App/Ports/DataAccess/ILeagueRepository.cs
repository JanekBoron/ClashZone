using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface ILeagueRepository
    {
        Task AddSeasonAsync(SeasonDomain season);
        Task<SeasonDomain?> GetSeasonByIdAsync(int id);
        Task<List<SeasonDomain>> GetSeasonsAsync();
        Task UpdateSeasonAsync(SeasonDomain season);

        Task<LeagueRegistrationDomain> AddRegistrationAsync(LeagueRegistrationDomain registration);
        Task<List<LeagueRegistrationDomain>> GetRegistrationsForSeasonAsync(int seasonId, bool includeRemoved = false);
        Task<LeagueRegistrationDomain?> GetRegistrationAsync(int seasonId, int leagueTeamId);
        Task<LeagueRegistrationDomain?> GetRegistrationByIdAsync(int id);
        Task SetRemovedAsync(int registrationId, bool removed);

        Task AddMatchAsync(LeagueMatchDomain match);
        Task<List<LeagueMatchDomain>> GetMatchesForSeasonAsync(int seasonId);
        Task<LeagueMatchDomain?> GetMatchByIdAsync(int id);
        Task UpdateMatchAsync(LeagueMatchDomain match);

        Task AddChatMessageAsync(ChatMessageDomain message);
        Task<List<ChatMessageDomain>> GetAllChatMessagesAsync(int seasonId);
        Task<List<ChatMessageDomain>> GetTeamChatMessagesAsync(int seasonId, int teamId);
        Task<List<ChatMessageDomain>> GetReportChatMessagesAsync(int seasonId, int? teamId, bool isAdmin);

        Task<List<PlayerMatchStatDomain>> GetPlayerStatsForLeagueMatchAsync(int leagueMatchId);
    }
}
