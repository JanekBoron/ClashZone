using System;
using System.ComponentModel.DataAnnotations;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Trwała drużyna ligowa, niezależna od konkretnego sezonu. W odróżnieniu od
    /// turniejowego <see cref="Team"/> (związanego z <c>TournamentId</c>) drużyna
    /// ligowa istnieje na stałe i może być rejestrowana do kolejnych sezonów ligi.
    /// Zarządzana przez użytkownika (kapitana) z poziomu Team Management.
    /// </summary>
    public class LeagueTeam
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        /// <summary>Identyfikator użytkownika będącego kapitanem (twórcą) drużyny.</summary>
        public string CaptainId { get; set; } = string.Empty;

        /// <summary>Kod dołączenia używany do zapraszania graczy linkiem.</summary>
        public string JoinCode { get; set; } = string.Empty;

        /// <summary>Tytuł gry drużyny (CS2 / League of Legends).</summary>
        public string Game { get; set; } = string.Empty;

        public string? LogoPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
