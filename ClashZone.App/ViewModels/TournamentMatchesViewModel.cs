using ClashZone.App.Domains;
using System.Collections.Generic;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// View model used for listing all matches belonging to a specific
    /// tournament.  Includes the tournament entity itself for
    /// contextual information and a collection of match list items
    /// prepared for display.
    /// </summary>
    public class TournamentMatchesViewModel
    {
        /// <summary>
        /// The tournament for which matches are being listed.
        /// </summary>
        public TournamentDomain Tournament { get; set; } = null!;

        /// <summary>
        /// Collection of match summaries with team names and scores.
        /// </summary>
        public List<MatchListItemViewModel> Matches { get; set; } = new();
    }
}