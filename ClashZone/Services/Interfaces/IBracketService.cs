using System.Threading.Tasks;
using ClashZone.ViewModels;

namespace ClashZone.Services.Interfaces
{
    /// <summary>
    /// Service interface responsible for generating tournament brackets and
    /// associated results.  Methods are provided to build an empty bracket,
    /// generate random scores for simulation and, optionally, generate
    /// detailed per-player statistics with persistence.
    /// </summary>
    public interface IBracketService
    {
        /// <summary>
        /// Generates a bracket structure for a tournament without any scores.
        /// </summary>
        Task<BracketViewModel?> GetBracketAsync(int tournamentId);

        /// <summary>
        /// Generates a bracket for the specified tournament with random scores
        /// and determines winners.  This is used when the user requests to
        /// view results without generating statistics.
        /// </summary>
        Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId);

        /// <summary>
        /// Generates a bracket with random results and produces random
        /// statistics (kills, deaths, assists) for each player in the
        /// tournament.  The statistics are persisted to the database and
        /// aggregated in the <see cref="UserStat"/> entity.  Returns the
        /// updated bracket for display.
        /// </summary>
        Task<BracketViewModel?> GetBracketWithStatsAsync(int tournamentId);
    }
}