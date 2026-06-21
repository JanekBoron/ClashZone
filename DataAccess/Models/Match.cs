using System;
using System.Collections.Generic;
using ClashZone.DataAccess.Models;

namespace DataAccess.Models
{
    public class Match
    {
        public int Id { get; set; }

        public int TournamentId { get; set; }

        public int? Team1Id { get; set; }

        public int? Team2Id { get; set; }

        public int Team1Score { get; set; }

        public int Team2Score { get; set; }

        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        public Tournament Tournament { get; set; } = null!;
        public Team? Team1 { get; set; }
        public Team? Team2 { get; set; }

        public List<ClashZone.DataAccess.Models.PlayerMatchStat> PlayerStats { get; set; } = new();
    }
}