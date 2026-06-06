using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    /// <summary>Wynik próby rejestracji drużyny do sezonu.</summary>
    public class LeagueRegistrationResult
    {
        public bool Success { get; set; }
        public bool SeasonNotFound { get; set; }
        public bool RegistrationClosed { get; set; }
        public bool RequiresLeaguePackage { get; set; }
        public bool NotCaptain { get; set; }
        public bool RosterTooSmall { get; set; }
        public bool AlreadyRegistered { get; set; }
        public int RequiredRoster { get; set; }
    }

    /// <summary>Wiersz tabeli ligowej (standings).</summary>
    public class LeagueStandingRow
    {
        public int RegistrationId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRatio => Played == 0 ? 0 : (double)Wins / Played;
    }

    /// <summary>Przegląd sezonu (zakładka „Przegląd") + dane do rejestracji.</summary>
    public class SeasonDetailsViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public int RegisteredTeamsCount { get; set; }
        public List<LeagueStandingRow> Standings { get; set; } = new();

        /// <summary>Czy użytkownik ma aktywny pakiet z dostępem do ligi.</summary>
        public bool HasLeagueAccess { get; set; }

        /// <summary>Drużyny użytkownika, które może zgłosić do tego sezonu.</summary>
        public List<LeagueTeamDomain> EligibleTeams { get; set; } = new();

        /// <summary>Czy któraś z drużyn użytkownika jest już zgłoszona.</summary>
        public bool IsRegistered { get; set; }
    }

    /// <summary>Pozycja na liście meczów ligowych.</summary>
    public class LeagueMatchListItemViewModel
    {
        public LeagueMatchDomain Match { get; set; } = null!;
        public string GameTitle { get; set; } = string.Empty;
        public string Team1Name { get; set; } = "BYE";
        public string Team2Name { get; set; } = "BYE";
        public string ScheduleLabel { get; set; } = string.Empty;
    }

    public class LeagueMatchesViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public List<LeagueMatchListItemViewModel> Matches { get; set; } = new();
    }

    /// <summary>Drabinka play-off (ten sam kształt co turniejowa).</summary>
    public class LeagueBracketViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public List<List<MatchInfoDomain>> Rounds { get; set; } = new();
    }

    /// <summary>Lista uczestników sezonu (zarejestrowane drużyny + rostery).</summary>
    public class LeagueParticipantsViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public List<LeagueParticipantTeamViewModel> Teams { get; set; } = new();
    }

    public class LeagueParticipantTeamViewModel
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool FeePaid { get; set; }
        public List<string> Members { get; set; } = new();
    }

    /// <summary>Reguły sezonu.</summary>
    public class LeagueRulesViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public List<string> Rules { get; set; } = new();
    }

    /// <summary>Podstawowy scheduling pojedynczego meczu ligowego.</summary>
    public class LeagueScheduleViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public LeagueMatchDomain Match { get; set; } = null!;
        public string Team1Name { get; set; } = string.Empty;
        public string Team2Name { get; set; } = string.Empty;
        public bool CanPropose { get; set; }
    }

    /// <summary>Czat sezonu (mirror <see cref="ChatViewModel"/>).</summary>
    public class LeagueChatViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public List<ChatMessageDomain> AllMessages { get; set; } = new();
        public List<ChatMessageDomain> TeamMessages { get; set; } = new();
        public List<ChatMessageDomain> ReportMessages { get; set; } = new();
        public bool HasTeam { get; set; }
        public int? UserTeamId { get; set; }
        public Dictionary<string, string> UserNames { get; set; } = new();
    }

    /// <summary>Szczegóły meczu ligowego ze statystykami graczy.</summary>
    public class LeagueMatchDetailsViewModel
    {
        public SeasonDomain Season { get; set; } = null!;
        public LeagueMatchDomain Match { get; set; } = null!;
        public string GameTitle { get; set; } = string.Empty;
        public string Team1Name { get; set; } = string.Empty;
        public string Team2Name { get; set; } = string.Empty;
        public string Team1ProfileUrl { get; set; } = "/images/default-profile.png";
        public string Team2ProfileUrl { get; set; } = "/images/default-profile.png";
        public List<PlayerStatViewModel> Team1Stats { get; set; } = new();
        public List<PlayerStatViewModel> Team2Stats { get; set; } = new();
    }
}
