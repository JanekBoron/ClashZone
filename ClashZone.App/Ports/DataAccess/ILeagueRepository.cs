using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    /// <summary>
    /// Dostęp do danych ligi: sezonów, zgłoszeń drużyn, meczów ligowych, czatu
    /// sezonu oraz statystyk graczy meczów ligowych.
    /// </summary>
    public interface ILeagueRepository
    {
        // --- Sezony ---
        Task AddSeasonAsync(SeasonDomain season);
        Task<SeasonDomain?> GetSeasonByIdAsync(int id);
        Task<List<SeasonDomain>> GetSeasonsAsync();
        Task UpdateSeasonAsync(SeasonDomain season);

        // --- Zgłoszenia (rejestracje) ---
        Task<LeagueRegistrationDomain> AddRegistrationAsync(LeagueRegistrationDomain registration);
        Task<List<LeagueRegistrationDomain>> GetRegistrationsForSeasonAsync(int seasonId, bool includeRemoved = false);
        Task<LeagueRegistrationDomain?> GetRegistrationAsync(int seasonId, int leagueTeamId);
        Task<LeagueRegistrationDomain?> GetRegistrationByIdAsync(int id);
        Task SetRemovedAsync(int registrationId, bool removed);

        // --- Mecze ligowe ---
        Task AddMatchAsync(LeagueMatchDomain match);
        Task<List<LeagueMatchDomain>> GetMatchesForSeasonAsync(int seasonId);
        Task<LeagueMatchDomain?> GetMatchByIdAsync(int id);
        Task UpdateMatchAsync(LeagueMatchDomain match);

        // --- Czat sezonu (reuse tabeli ChatMessage) ---
        Task AddChatMessageAsync(ChatMessageDomain message);
        Task<List<ChatMessageDomain>> GetAllChatMessagesAsync(int seasonId);
        Task<List<ChatMessageDomain>> GetTeamChatMessagesAsync(int seasonId, int teamId);
        Task<List<ChatMessageDomain>> GetReportChatMessagesAsync(int seasonId, int? teamId, bool isAdmin);

        // --- Statystyki graczy meczów ligowych ---
        Task<List<PlayerMatchStatDomain>> GetPlayerStatsForLeagueMatchAsync(int leagueMatchId);
    }
}
