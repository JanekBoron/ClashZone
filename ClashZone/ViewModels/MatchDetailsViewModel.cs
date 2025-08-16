using System.Collections.Generic;
using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used for the match details page.  Contains the
    /// underlying <see cref="DataAccess.Models.Match"/>, lists of
    /// per‑player statistics for each team, and convenience properties
    /// for accessing team names and captain profile images.
    /// </summary>
    public class MatchDetailsViewModel
    {
        /// <summary>
        /// The match entity containing scores and references to teams.
        /// </summary>
        public Match Match { get; set; } = null!;

        /// <summary>
        /// List of player statistics for the first team.
        /// </summary>
        public List<PlayerStatViewModel> Team1Stats { get; set; } = new();

        /// <summary>
        /// List of player statistics for the second team.
        /// </summary>
        public List<PlayerStatViewModel> Team2Stats { get; set; } = new();

        /// <summary>
        /// Display name for the first team.  Provided by the controller.
        /// </summary>
        public string Team1Name { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the second team.
        /// </summary>
        public string Team2Name { get; set; } = string.Empty;

        /// <summary>
        /// URL to the profile image of the first team's captain.
        /// </summary>
        public string Team1ProfileUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL to the profile image of the second team's captain.
        /// </summary>
        public string Team2ProfileUrl { get; set; } = string.Empty;
    }
}