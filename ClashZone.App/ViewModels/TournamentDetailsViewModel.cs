using ClashZone.App.Domains;
using System.Collections.Generic;
using System.Linq;

namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// View model used for rendering the tournament details page.  It
    /// combines the core tournament information with the user's team (if
    /// any), the team roster with readiness state and the matches that
    /// have been played in the tournament.
    /// </summary>
    public class TournamentDetailsViewModel
    {
        /// <summary>
        /// The tournament being displayed.
        /// </summary>
        public TournamentDomain Tournament { get; set; } = null!;

        /// <summary>
        /// The team the current user belongs to in this tournament, if any.
        /// </summary>
        public TeamDomain? UserTeam { get; set; }

        /// <summary>
        /// Roster of the user's team with each member's readiness state.
        /// </summary>
        public List<TournamentTeamMemberView> Members { get; set; } = new();

        /// <summary>
        /// Collection of matches that have been completed in this tournament.
        /// </summary>
        public List<MatchListItemViewModel> Matches { get; set; } = new();

        /// <summary>Id zalogowanego użytkownika (do wyróżnienia jego wiersza/akcji).</summary>
        public string? CurrentUserId { get; set; }

        /// <summary>Czy zalogowany użytkownik jest kapitanem swojej drużyny.</summary>
        public bool IsCaptain { get; set; }

        /// <summary>Wymagana liczba graczy w pełnym składzie (z formatu turnieju).</summary>
        public int RequiredRosterSize { get; set; }

        /// <summary>Czy drużyna użytkownika została już potwierdzona (jest w drabince).</summary>
        public bool IsTeamConfirmed { get; set; }

        /// <summary>Czy turniej osiągnął limit potwierdzonych drużyn.</summary>
        public bool TournamentFull { get; set; }

        /// <summary>Czy kapitan może w tej chwili potwierdzić drużynę.</summary>
        public bool CanConfirm { get; set; }

        /// <summary>Czy wszyscy obecni członkowie są gotowi.</summary>
        public bool AllReady => Members.Count > 0 && Members.All(m => m.Ready);

        /// <summary>Czy skład jest pełny (liczba członków == wymagany rozmiar).</summary>
        public bool RosterFull => Members.Count >= RequiredRosterSize;
    }

    /// <summary>Pojedynczy członek drużyny turniejowej wraz ze statusem gotowości.</summary>
    public class TournamentTeamMemberView
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Ready { get; set; }
    }
}
