using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    /// <summary>
    /// Defines an abstraction for retrieving match-related data from the
    /// persistence layer.  Repositories hide the underlying database
    /// implementation from services and controllers, enabling easier
    /// testing and separation of concerns.  All methods are asynchronous
    /// to avoid blocking threads during I/O bound operations.
    /// </summary>
    public interface IMatchesRepository
    {
        /// <summary>
        /// Retrieves all matches that have been played for a specific
        /// tournament.  Matches are ordered by the time they were
        /// played in descending order (most recent first).
        /// </summary>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <returns>A list of <see cref="Match"/> entities.</returns>
        Task<List<Match>> GetMatchesByTournamentAsync(int tournamentId);

        /// <summary>
        /// Retrieves a single match by its identifier and the
        /// tournament it belongs to.  Returns <c>null</c> when the
        /// match does not exist or does not belong to the specified
        /// tournament.
        /// </summary>
        /// <param name="matchId">Identifier of the match.</param>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <returns>The match entity or null.</returns>
        Task<Match?> GetMatchByIdAsync(int matchId, int tournamentId);

        /// <summary>
        /// Retrieves all per-player statistics recorded for a given
        /// match.  These statistics include kills, deaths and assists
        /// for each player that participated in the match.
        /// </summary>
        /// <param name="matchId">Identifier of the match.</param>
        /// <returns>A list of <see cref="PlayerMatchStat"/> entities.</returns>
        Task<List<PlayerMatchStat>> GetPlayerStatsForMatchAsync(int matchId);

        /// <summary>
        /// Retrieves the team entity with the specified identifier.
        /// Returns <c>null</c> when the team does not exist.
        /// </summary>
        /// <param name="teamId">Identifier of the team.</param>
        /// <returns>The team entity or null.</returns>
        Task<Team?> GetTeamByIdAsync(int teamId);
    }
}