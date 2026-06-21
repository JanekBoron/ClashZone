using ClashZone.App.Domains;
using System.Collections.Generic;
using System.Linq;

namespace ClashZone.App.ViewModels
{
    public class TournamentDetailsViewModel
    {
        public TournamentDomain Tournament { get; set; } = null!;
        public TeamDomain? UserTeam { get; set; }
        public List<TournamentTeamMemberView> Members { get; set; } = new();
        public List<MatchListItemViewModel> Matches { get; set; } = new();
        public string? CurrentUserId { get; set; }
        public bool IsCaptain { get; set; }
        public int RequiredRosterSize { get; set; }
        public bool IsTeamConfirmed { get; set; }
        public bool TournamentFull { get; set; }
        public bool CanConfirm { get; set; }
        public bool AllReady => Members.Count > 0 && Members.All(m => m.Ready);
        public bool RosterFull => Members.Count >= RequiredRosterSize;
    }
    public class TournamentTeamMemberView
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Ready { get; set; }
    }
}
