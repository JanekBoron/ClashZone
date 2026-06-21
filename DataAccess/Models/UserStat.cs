using System;
using System.ComponentModel.DataAnnotations.Schema;
using ClashZone.DataAccess.Models;

namespace ClashZone.DataAccess.Models
{
    public class UserStat
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public string Game { get; set; } = string.Empty;

        public int TotalKills { get; set; }

        public int TotalDeaths { get; set; }

        public int TotalAssists { get; set; }

        public int MatchesPlayed { get; set; }

        [NotMapped]
        public double AverageKD => TotalDeaths != 0 ? Math.Round((double)TotalKills / TotalDeaths, 2) : TotalKills;

        public ClashUser User { get; set; } = null!;
    }
}