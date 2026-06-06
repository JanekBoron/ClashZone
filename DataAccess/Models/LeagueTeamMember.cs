using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClashZone.DataAccess.Models
{
    /// <summary>
    /// Łączy użytkownika (Identity ID) z trwałą drużyną ligową
    /// <see cref="LeagueTeam"/>. <see cref="JoinedAt"/> umożliwia egzekwowanie
    /// okresów karencji (cooldown) przy dołączaniu do rosteru.
    /// </summary>
    public class LeagueTeamMember
    {
        [Key]
        public int Id { get; set; }

        public int LeagueTeamId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(LeagueTeamId))]
        public LeagueTeam LeagueTeam { get; set; } = default!;
    }
}
