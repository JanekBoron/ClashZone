using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model representing a single match item in the list of
    /// previously played tournament matches.  This type aggregates
    /// information from the <see cref="DataAccess.Models.Match"/> entity
    /// along with team names and captain profile images for display
    /// purposes.  The profile URLs may refer to custom avatars or
    /// fallback to the default silhouette if none has been uploaded.
    /// </summary>
    public class MatchListItemViewModel
    {
        /// <summary>
        /// Underlying match entity containing scores and team IDs.
        /// </summary>
        public Match Match { get; set; } = null!;

        /// <summary>
        /// Display name for the first team.  Derived from the team's Name
        /// property or generated from the captain's username when no
        /// explicit name has been assigned.
        /// </summary>
        public string Team1Name { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the second team.
        /// </summary>
        public string Team2Name { get; set; } = string.Empty;

        /// <summary>
        /// URL to the profile image of the first team's captain.  Can be a
        /// relative path within the application (e.g. "/images/profiles/xyz.png").
        /// </summary>
        public string Team1ProfileUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL to the profile image of the second team's captain.
        /// </summary>
        public string Team2ProfileUrl { get; set; } = string.Empty;
    }
}