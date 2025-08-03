using ClashZone.DataAccess.Models;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    /// <summary>
    /// Contract for retrieving and manipulating tournament data.  In addition to
    /// listing upcoming tournaments and tournaments for a specific user, this
    /// interface exposes a method for retrieving a single tournament by its
    /// primary key.
    /// </summary>
    public interface ITournamentsRepository
    {
        /// <summary>
        /// Retrieves a list of upcoming tournaments.  If <paramref name="format"/>
        /// is not empty, the results are filtered by the specified team size
        /// (e.g. "1v1", "2v2", "5v5").
        /// </summary>
        Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync(string? format);

        /// <summary>
        /// Retrieves tournaments in which the given user participates or has
        /// registered.  If a join table is not implemented, implementations
        /// should return tournaments created by the user (<see
        /// cref="Tournament.CreatedByUserId"/>).
        /// </summary>
        Task<IEnumerable<Tournament>> GetUserTournamentsAsync(string userId);

        /// <summary>
        /// Adds a new tournament to the underlying data store.
        /// </summary>
        Task AddTournamentAsync(Tournament tournament);

        /// <summary>
        /// Retrieves a single tournament by its identifier.  Returns <c>null</c>
        /// if no tournament with the provided ID exists.
        /// </summary>
        /// <param name="id">Primary key of the tournament.</param>
        Task<Tournament?> GetTournamentByIdAsync(int id);

        // ---------------------------------------------------------------------
        // Team management methods.  These methods encapsulate operations
        // related to creating and joining teams for tournaments.  They hide
        // direct access to the database context from the controller.

        /// <summary>
        /// Returns the team that the specified user belongs to within a given
        /// tournament.  If the user is not yet part of any team, returns
        /// <c>null</c>.
        /// </summary>
        Task<Team?> GetUserTeamAsync(int tournamentId, string userId);

        /// <summary>
        /// Returns a list of user identifiers for all members of the given team.
        /// </summary>
        Task<List<string>> GetTeamMemberIdsAsync(int teamId);

        /// <summary>
        /// Creates a new team for the specified tournament and assigns the
        /// given user as its captain.  A new join code is generated for the
        /// team.  The captain is also added as the first member of the team.
        /// </summary>
        Task<Team> CreateTeamWithCaptainAsync(int tournamentId, string userId);

        /// <summary>
        /// Adds a user to an existing team.  The provided code must match the
        /// team's join code.  Returns the tournament identifier if the
        /// operation succeeds.  Returns <c>null</c> if the team does not
        /// exist or the join code is invalid.  If the user is already a
        /// member of the team, the operation is a no‑op and also returns
        /// the tournament identifier.
        /// </summary>
        Task<int?> AddUserToTeamAsync(int teamId, string userId, string code);

        // ---------------------------------------------------------------------
        // Chat related methods

        /// <summary>
        /// Persists a chat message to the underlying data store.
        /// </summary>
        Task AddChatMessageAsync(ChatMessage message);

        /// <summary>
        /// Retrieves all chat messages visible to all participants of a
        /// tournament (TeamId is null), ordered by timestamp ascending.
        /// </summary>
        Task<List<ChatMessage>> GetAllChatMessagesAsync(int tournamentId);

        /// <summary>
        /// Retrieves chat messages for a specific team within a tournament,
        /// ordered by timestamp ascending.
        /// </summary>
        Task<List<ChatMessage>> GetTeamChatMessagesAsync(int tournamentId, int teamId);
    }
}
