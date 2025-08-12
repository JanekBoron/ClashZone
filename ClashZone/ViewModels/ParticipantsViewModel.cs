using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model for displaying all participants (teams) in a tournament.
    /// It contains the tournament itself and a list of teams with their
    /// display names and members.  This model is used by the Uczestnicy
    /// (Participants) view to render the participants tab.
    /// </summary>
    public class ParticipantsViewModel
    {
        /// <summary>
        /// Tournament context for which participants are displayed.
        /// </summary>
        public Tournament Tournament { get; set; } = null!;

        /// <summary>
        /// Collection of teams participating in the tournament.  Each entry
        /// includes a friendly name and the list of member display names.
        /// </summary>
        public List<ParticipantTeamViewModel> Teams { get; set; } = new();
    }

    /// <summary>
    /// Represents a single team within the participants tab.  Contains the
    /// unique identifier, friendly name and a collection of member names.
    /// </summary>
    public class ParticipantTeamViewModel
    {
        /// <summary>
        /// Primary key of the team.
        /// </summary>
        public int TeamId { get; set; }

        /// <summary>
        /// Friendly display name for the team.  If the team has not set a
        /// custom name, this will fall back to the captain's username or a
        /// generated placeholder.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// List of display names for users on the team.  May be empty if
        /// membership has not yet been populated.
        /// </summary>
        public List<string> Members { get; set; } = new();
    }
}
