using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Zgłoszenie trwałej drużyny ligowej <see cref="LeagueTeam"/> do konkretnego
    /// <see cref="Season"/>. Po zamknięciu rejestracji drużyny niespełniające
    /// wymogów (roster / opłacone wpisowe) są oznaczane jako usunięte.
    /// </summary>
    public class LeagueRegistration
    {
        [Key]
        public int Id { get; set; }

        public int SeasonId { get; set; }

        public int LeagueTeamId { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        /// <summary>Czy wpisowe zostało opłacone (powiązane z pakietem ligi).</summary>
        public bool FeePaid { get; set; }

        /// <summary>Czy drużyna została usunięta z sezonu (np. po przycięciu rejestracji).</summary>
        public bool IsRemoved { get; set; }

        [ForeignKey(nameof(LeagueTeamId))]
        public LeagueTeam LeagueTeam { get; set; } = default!;
    }
}
