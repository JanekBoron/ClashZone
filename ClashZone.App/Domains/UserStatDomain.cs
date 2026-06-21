using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class UserStatDomain
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Game { get; set; } = string.Empty;
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalAssists { get; set; }
        public int MatchesPlayed { get; set; }
        public double AverageKD => TotalDeaths != 0 ? Math.Round((double)TotalKills / TotalDeaths, 2) : TotalKills;
        public double AverageKDA => Math.Round((double)(TotalKills + TotalAssists) / (TotalDeaths != 0 ? TotalDeaths : 1), 2);
        public ClashUserDomain User { get; set; } = null!;
    }
}
