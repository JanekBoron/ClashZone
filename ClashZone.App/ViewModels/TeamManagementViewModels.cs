using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    /// <summary>Drużyny założone przez użytkownika: ligowe oraz turniejowe (osobno).</summary>
    public class TeamManagementViewModel
    {
        /// <summary>Trwałe drużyny ligowe, których użytkownik jest kapitanem.</summary>
        public List<LeagueTeamDomain> Teams { get; set; } = new();

        /// <summary>Drużyny turniejowe założone przez użytkownika.</summary>
        public List<TournamentTeamListItem> TournamentTeams { get; set; } = new();
    }

    /// <summary>Pozycja listy drużyn turniejowych użytkownika (kapitana).</summary>
    public class TournamentTeamListItem
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string TournamentName { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public int TournamentId { get; set; }
        public bool IsConfirmed { get; set; }
    }

    /// <summary>Formularz tworzenia nowej drużyny ligowej.</summary>
    public class LeagueTeamFormViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
    }

    /// <summary>Szczegóły drużyny ligowej: roster, link zaproszenia, rola.</summary>
    public class LeagueTeamDetailsViewModel
    {
        public LeagueTeamDomain Team { get; set; } = null!;
        public List<string> MemberNames { get; set; } = new();
        public bool IsCaptain { get; set; }
        public bool IsMember { get; set; }
        public string? InviteLink { get; set; }
    }

    /// <summary>Wynik próby dołączenia do drużyny przez link/kod.</summary>
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
