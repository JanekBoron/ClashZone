using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used for the chat page.  Contains the tournament details,
    /// messages visible to all participants, messages visible to the user's
    /// team, and a flag indicating whether the user belongs to a team.
    /// </summary>
    public class ChatViewModel
    {
        public Tournament Tournament { get; set; } = default!;
        public List<ChatMessage> AllMessages { get; set; } = new List<ChatMessage>();
        public List<ChatMessage> TeamMessages { get; set; } = new List<ChatMessage>();
        public bool HasTeam { get; set; }

        /// <summary>
        /// Mapping of user identifiers to display names.  Used for showing
        /// readable names next to messages.  Filled by the controller.
        /// </summary>
        public Dictionary<string, string> UserNames { get; set; } = new Dictionary<string, string>();
    }
}
