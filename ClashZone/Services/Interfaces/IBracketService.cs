using System.Threading.Tasks;
using ClashZone.ViewModels;

namespace ClashZone.Services.Interfaces
{
    /// <summary>
    /// Service interface responsible for generating tournament brackets and
    /// associated results.  Methods are provided to build an empty bracket,
    /// generate random scores for simulation and, optionally, generate
    /// detailed per?player statistics with persistence.
    /// </summary>
    public interface IBracketService
    {
        /// <summary>
        /// Generates a bracket structure for a tournament without any scores.
        /// </summary>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <returns>A view model representing the bracket or null when no bracket can be generated.</returns>
        Task<BracketViewModel?> GetBracketAsync(int tournamentId);

        /// <summary>
        /// Generates a bracket for the specified tournament with random scores
        /// and determines winners.  This is used when the user requests to
        /// view results without generating statistics.
        /// </summary>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <returns>A view model representing the bracket with randomly assigned results.</returns>
        Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId);

        /// <summary>
        /// Generates a bracket with random results and produces random
        /// statistics (kills, deaths, assists) for each player in the
        /// tournament.  The statistics are persisted to the database and
        /// aggregated in the user statistics entity.  Returns the
        /// updated bracket for display.
        /// </summary>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <returns>A view model representing the bracket with results and persisted statistics.</returns>
        Task<BracketViewModel?> GetBracketWithStatsAsync(int tournamentId);

        /// <summary>
        /// Simulates a single match within an existing bracket.  The match is identified
        /// by its round number and match number (both one?based).  A random score
        /// is assigned to each team and the result is persisted to the database.
        /// If both teams have not yet been determined (for example the previous round
        /// has unplayed matches) the simulation is aborted and null is returned.
        /// After simulation the bracket is rebuilt to include the new result.
        /// </summary>
        /// <param name="tournamentId">The tournament whose bracket should be updated.</param>
        /// <param name="roundNumber">The one?based round index of the match to simulate.</param>
        /// <param name="matchNumber">The one?based index of the match within the round.</param>
        /// <returns>The updated bracket view model with the new result applied, or null if the simulation could not be performed.</returns>
        Task<BracketViewModel?> SimulateMatchAsync(int tournamentId, int roundNumber, int matchNumber);
    }
}