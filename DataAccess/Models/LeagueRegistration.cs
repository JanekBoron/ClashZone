using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClashZone.DataAccess.Models
{
    public class LeagueRegistration
    {
        [Key]
        public int Id { get; set; }

        public int SeasonId { get; set; }

        public int LeagueTeamId { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public bool FeePaid { get; set; }

        public bool IsRemoved { get; set; }

        [ForeignKey(nameof(LeagueTeamId))]
        public LeagueTeam LeagueTeam { get; set; } = default!;
    }
}
