using System;
using System.ComponentModel.DataAnnotations;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Mecz ligowy — zarówno z sezonu regularnego (przypisany do tygodnia), jak
    /// i z fazy play-off (<see cref="IsPlayoff"/> + <see cref="Round"/>). Drużyny
    /// reprezentowane są przez identyfikatory zgłoszeń <see cref="LeagueRegistration"/>.
    /// Pola schedulingu pozwalają zaproponować termin/serwer inny niż domyślny.
    /// </summary>
    public class LeagueMatch
    {
        [Key]
        public int Id { get; set; }

        public int SeasonId { get; set; }

        /// <summary>Numer tygodnia sezonu regularnego (0 dla play-off).</summary>
        public int Week { get; set; }

        public int? HomeRegistrationId { get; set; }
        public int? AwayRegistrationId { get; set; }

        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool IsPlayed { get; set; }

        /// <summary>Domyślny/uzgodniony termin meczu.</summary>
        public DateTime ScheduledTime { get; set; }

        // --- Scheduling (podstawowy): propozycja zmiany terminu/serwera ---
        public DateTime? ProposedTime { get; set; }
        public int? ProposedByRegistrationId { get; set; }
        public string? Server { get; set; }

        public bool IsPlayoff { get; set; }

        /// <summary>Numer rundy play-off (1 = pierwsza runda).</summary>
        public int? Round { get; set; }
    }
}
