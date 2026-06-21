using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.App.Domains
{
    public class PlayerMatchStatDomain
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
        public MatchDomain? Match { get; set; }
        public ClashUserDomain User { get; set; } = null!;
        public double KD => Deaths != 0 ? Math.Round((double)Kills / Deaths, 2) : Kills;
    }
}
