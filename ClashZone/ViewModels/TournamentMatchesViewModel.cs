using System.Collections.Generic;
using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
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
        public Tournament Tournament { get; set; } = null!;

        /// <summary>
        /// Collection of match summaries with team names and scores.
        /// </summary>
        public List<MatchListItemViewModel> Matches { get; set; } = new();
    }
}