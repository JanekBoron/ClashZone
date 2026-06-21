

using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface ITournamentsRepository
    {
        Task<IEnumerable<TournamentDomain>> GetUpcomingTournamentsAsync(string? format);
        Task<IEnumerable<TournamentDomain>> GetUserTournamentsAsync(string userId);
        Task AddTournamentAsync(TournamentDomain tournament);
        Task<TournamentDomain?> GetTournamentByIdAsync(int id);
        Task<List<ChatMessageDomain>> GetReportChatMessagesAsync(int tournamentId, int? teamId, bool isAdmin);
        Task UpdateTournamentAsync(TournamentDomain tournament);
        Task DeleteTournamentAsync(int id);

        Task<bool> TryMarkInitialMatchRemindersSentAsync(int tournamentId);

        Task<TeamDomain?> GetUserTeamAsync(int tournamentId, string userId);
        Task<List<string>> GetTeamMemberIdsAsync(int teamId);
        Task<List<TeamMemberDomain>> GetTeamMembersAsync(int teamId);
        Task<TeamDomain> CreateTeamWithCaptainAsync(int tournamentId, string userId);
        Task<int?> AddUserToTeamAsync(int teamId, string userId, string code);
        Task SetMemberReadyAsync(int teamId, string userId, bool ready);
        Task ConfirmTeamAsync(int teamId);
        Task<int> GetConfirmedTeamCountAsync(int tournamentId);
        Task<List<TeamDomain>> GetCaptainTeamsAsync(string userId);


        Task AddChatMessageAsync(ChatMessageDomain message);
        Task<List<ChatMessageDomain>> GetAllChatMessagesAsync(int tournamentId);
        Task<List<ChatMessageDomain>> GetTeamChatMessagesAsync(int tournamentId, int teamId);
        Task<List<TeamDomain>> GetTeamsForTournamentAsync(int tournamentId);
    }
}