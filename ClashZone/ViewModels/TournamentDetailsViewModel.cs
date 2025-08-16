using System.Collections.Generic;
using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used for rendering the tournament details page.  It
    /// combines the core tournament information with the user's team (if
    /// any), the list of team member display names and the matches that
    /// have been played in the tournament.
    /// </summary>
    public class TournamentDetailsViewModel
    {
        /// <summary>
        /// The tournament being displayed.
        /// </summary>
        public Tournament Tournament { get; set; } = null!;

        /// <summary>
        /// The team the current user belongs to in this tournament, if any.
        /// </summary>
        public Team? UserTeam { get; set; }

        /// <summary>
        /// Collection of display names for the members of the user's team.  The
        /// names are resolved from the associated <see cref="ClashUser"/>
        /// entities via the tournament service.
        /// </summary>
        public List<string> TeamMembers { get; set; } = new();

        /// <summary>
        /// Collection of matches that have been completed in this tournament.  Each
        /// item provides friendly team names, scores and avatar URLs for display.
        /// </summary>
        public List<MatchListItemViewModel> Matches { get; set; } = new();
    }
}