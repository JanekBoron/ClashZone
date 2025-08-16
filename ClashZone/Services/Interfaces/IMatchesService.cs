using System.Collections.Generic;
using System.Threading.Tasks;
using ClashZone.ViewModels;

namespace ClashZone.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for business logic related to tournament
    /// matches.  Services coordinate between repositories and other
    /// application services to assemble view models that can be
    /// consumed by controllers and views.  They hide the details of
    /// data access and entity relationships from higher layers.
    /// </summary>
    public interface IMatchesService
    {
        /// <summary>
        /// Builds a collection of match list items for the specified
        /// tournament.  Each item includes friendly team names, scores
        /// and captain profile pictures.  When no matches exist an
        /// empty collection is returned.
        /// </summary>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <returns>A collection of <see cref="MatchListItemViewModel"/>.</returns>
        Task<List<MatchListItemViewModel>> GetMatchesForTournamentAsync(int tournamentId);

        /// <summary>
        /// Constructs a detailed view model for a single match,
        /// including per‑player statistics for each team, team names
        /// and captain avatars.  Returns <c>null</c> if the match
        /// cannot be found.
        /// </summary>
        /// <param name="tournamentId">Identifier of the tournament.</param>
        /// <param name="matchId">Identifier of the match.</param>
        /// <returns>A <see cref="MatchDetailsViewModel"/> or null.</returns>
        Task<MatchDetailsViewModel?> GetMatchDetailsAsync(int tournamentId, int matchId);
    }
}