using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used for rendering the tournament details page.  It
    /// combines the core tournament information with the user's team (if
    /// any) and the list of team member display names.
    /// </summary>
    public class TournamentDetailsViewModel
    {
        public Tournament Tournament { get; set; } = null!;

        public Team? UserTeam { get; set; }

        public List<string> TeamMembers { get; set; } = new();
    }
}
