using System;
using ClashZone.DataAccess.Models;
using DataAccess.Models;

namespace ClashZone.DataAccess.Models
{
    public class PlayerMatchStat
    {
        public int Id { get; set; }

        public int? MatchId { get; set; }

        public int? LeagueMatchId { get; set; }

        public string UserId { get; set; } = null!;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }

        public string Game { get; set; } = string.Empty;

        public string? Champion { get; set; }

        public Match? Match { get; set; }
        public ClashUser User { get; set; } = null!;

        public double KD => Deaths != 0 ? Math.Round((double)Kills / Deaths, 2) : Kills;
    }
}