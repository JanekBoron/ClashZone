using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used for the chat page.  Contains the tournament details,
    /// messages visible to all participants, messages visible to the user's
    /// team, messages reserved for problem reports, a flag indicating
    /// whether the user belongs to a team and the user's team identifier.
    /// 
    /// The ReportMessages collection contains chat messages where
    /// <see cref="ChatMessage.IsReport"/> is true.  These messages are
    /// visible to the team's members and to tournament administrators.
    /// </summary>
    public class ChatViewModel
    {
        public Tournament Tournament { get; set; } = default!;
        public List<ChatMessage> AllMessages { get; set; } = new List<ChatMessage>();
        public List<ChatMessage> TeamMessages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Collection of problem report messages.  Each message has
        /// IsReport = true and TeamId equal to the user's team.  When the
        /// current user is an administrator, this list contains all
        /// report messages for the tournament.
        /// </summary>
        public List<ChatMessage> ReportMessages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Indicates whether the current user is assigned to a team in this
        /// tournament.  If false, team‑specific chats will be disabled.
        /// </summary>
        public bool HasTeam { get; set; }

        /// <summary>
        /// The identifier of the team that the current user belongs to, if any.
        /// </summary>
        public int? UserTeamId { get; set; }

        /// <summary>
        /// Mapping of user identifiers to display names.  Used for showing
        /// readable names next to messages.  Filled by the chat service.
        /// </summary>
        public Dictionary<string, string> UserNames { get; set; } = new Dictionary<string, string>();
    }
}
