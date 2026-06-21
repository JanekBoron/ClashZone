using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class TeamManagementViewModel
    {
        public List<LeagueTeamDomain> Teams { get; set; } = new();
        public List<TournamentTeamListItem> TournamentTeams { get; set; } = new();
    }

    public class TournamentTeamListItem
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TournamentName { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public int TournamentId { get; set; }
        public bool IsConfirmed { get; set; }
    }

    public class LeagueTeamFormViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
    }

    public class LeagueTeamDetailsViewModel
    {
        public LeagueTeamDomain Team { get; set; } = null!;
        public List<string> MemberNames { get; set; } = new();
        public bool IsCaptain { get; set; }
        public bool IsMember { get; set; }
        public string? InviteLink { get; set; }
    }

    public class JoinLeagueTeamResult
    {
        public bool NotFound { get; set; }
        public bool InvalidCode { get; set; }
        public bool AlreadyMember { get; set; }
        public bool UserCooldown { get; set; }
        public bool RosterCooldown { get; set; }
        public bool Success { get; set; }
        public int TeamId { get; set; }
    }
}
