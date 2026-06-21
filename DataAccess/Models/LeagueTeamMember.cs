using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClashZone.DataAccess.Models
{
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
