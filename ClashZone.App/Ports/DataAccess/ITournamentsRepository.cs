

using ClashZone.App.Domains;

namespace ClashZone.App.Ports.DataAccess
{
    public interface ITournamentsRepository
    {
        Task<IEnumerable<TournamentDomain>> GetUpcomingTournamentsAsync(string? format);

        Task<IEnumerable<TournamentDomain>> GetUserTournamentsAsync(string userId);

        Task AddTournamentAsync(TournamentDomain tournament);

        Task<TournamentDomain?> GetTournamentByIdAsync(int id);

        /// <summary>
        /// Pobiera wiadomości z czatu zgłoszeń dla podanego turnieju.
        /// Administratorzy mogą pobrać zgłoszenia dla wszystkich drużyn (teamId = null),
        /// natomiast zwykli użytkownicy dostają zgłoszenia wyłącznie własnej drużyny.
        /// </summary>
        /// <param name="tournamentId">Identyfikator turnieju.</param>
        /// <param name="teamId">Identyfikator drużyny lub null w przypadku administratora.</param>
        /// <param name="isAdmin">Czy użytkownik jest administratorem.</param>
        Task<List<ChatMessageDomain>> GetReportChatMessagesAsync(int tournamentId, int? teamId, bool isAdmin);

        /// <summary>
        /// Updates an existing tournament.  Persists the changes to the database.
        /// </summary>
        /// <param name="tournament">The tournament entity with updated values.</param>
        Task UpdateTournamentAsync(TournamentDomain tournament);

        /// <summary>
        /// Deletes the tournament with the given identifier.
        /// </summary>
        /// <param name="id">Identifier of the tournament to remove.</param>
        Task DeleteTournamentAsync(int id);

        // ---------------------------------------------------------------------
        // Team management methods.  These methods encapsulate operations
        // related to creating and joining teams for tournaments.  They hide
        // direct access to the database context from the controller.

        Task<TeamDomain?> GetUserTeamAsync(int tournamentId, string userId);

        Task<List<string>> GetTeamMemberIdsAsync(int teamId);

        /// <summary>Członkowie drużyny wraz z ich statusem gotowości (IsReady).</summary>
        Task<List<TeamMemberDomain>> GetTeamMembersAsync(int teamId);

        Task<TeamDomain> CreateTeamWithCaptainAsync(int tournamentId, string userId);

        Task<int?> AddUserToTeamAsync(int teamId, string userId, string code);

        /// <summary>Ustawia gotowość pojedynczego członka drużyny.</summary>
        Task SetMemberReadyAsync(int teamId, string userId, bool ready);

        /// <summary>Oznacza drużynę jako potwierdzoną (wchodzi do drabinki).</summary>
        Task ConfirmTeamAsync(int teamId);

        /// <summary>Liczba potwierdzonych drużyn w turnieju (= zajęte sloty).</summary>
        Task<int> GetConfirmedTeamCountAsync(int tournamentId);

        /// <summary>Drużyny turniejowe, których podany użytkownik jest kapitanem.</summary>
        Task<List<TeamDomain>> GetCaptainTeamsAsync(string userId);

        // ---------------------------------------------------------------------
        // Chat related methods

        Task AddChatMessageAsync(ChatMessageDomain message);

        Task<List<ChatMessageDomain>> GetAllChatMessagesAsync(int tournamentId);

        Task<List<ChatMessageDomain>> GetTeamChatMessagesAsync(int tournamentId, int teamId);

        Task<List<TeamDomain>> GetTeamsForTournamentAsync(int tournamentId);
    }
}